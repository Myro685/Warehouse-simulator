using System.Collections;
using System.Collections.Generic; // Přidat pro List
using UnityEngine;
using Warehouse.Grid;
using Warehouse.Pathfinding; // Přidat namespace

namespace Warehouse.Units
{
    public class AGVController : MonoBehaviour
    {
        [Header("Movement Settings")]
        [SerializeField] private float _moveSpeed = 2.0f;

        private LineRenderer _lineRenderer;

        public GridNode CurrentNode { get; private set; }
        public bool IsMoving { get; private set; }

        private void Awake()
        {
            _lineRenderer = GetComponent<LineRenderer>();
            _lineRenderer.positionCount = 0;
        }

        public void Initialize(GridNode startNode)
        {
            CurrentNode = startNode;
            Vector3 startPos = startNode.WorldPosition;
            startPos.y = 0.5f;
            transform.position = startPos;
        }

        /// <summary>
        /// Zadá vozíku cílový bod. Vozík si sám vypočítá cestu.
        /// </summary>
        public void SetDestination(GridNode targetNode)
        {
            // 1. Vypočítat cestu
            List<GridNode> path = Pathfinder.FindPath(CurrentNode, targetNode);

            if (path != null && path.Count > 0)
            {
                DrawPath(path);

                // 2. Spustit pohyb po cestě
                StopAllCoroutines(); // Zastavit předchozí pohyb
                StartCoroutine(FollowPath(path));
            }
            else
            {
                Debug.LogWarning("Cesta nenalezena!");
            }
        }

        /// <summary>
        /// Nastaví body pro LineRenderer.
        /// </summary>
        private void DrawPath(List<GridNode> path)
        {
            if(_lineRenderer == null) return;

            // LineRenderer potřebuje Vector3 pole.
            // Přidáme i aktuální pozici jako startovní bod (index 0)
            _lineRenderer.positionCount = path.Count + 1;

            // 1. bod je aktuální pozice vozíku
            _lineRenderer.SetPosition(0, transform.position);

            // Další body jsou středy nodů
            for (int i = 0; i < path.Count; i++)
            {
                Vector3 pos = path[i].WorldPosition;
                pos.y = 0.5f;
                _lineRenderer.SetPosition(i + 1, pos);
            }
        }

        /// <summary>
        /// Nastaví body pro LineRenderer.
        /// Bere aktuální pozici vozíku + zbytek cesty.
        /// </summary>
        private void UpdatePathVisual(List<GridNode> remainingPath)
        {
            if (_lineRenderer == null) return;

            // Pokud už není cesta, vymažeme čáru
            if (remainingPath == null || remainingPath.Count == 0)
            {
                _lineRenderer.positionCount = 0;
                return;
            }

            // Počet bodů = aktuální pozice vozíku + počet zbývajících uzlů
            _lineRenderer.positionCount = remainingPath.Count + 1;
            
            // 1. bod je VŽDY aktuální pozice vozíku (aby čára nebyla "utržená")
            _lineRenderer.SetPosition(0, transform.position);

            // Další body jsou středy nodů z cesty
            for (int i = 0; i < remainingPath.Count; i++)
            {
                Vector3 pos = remainingPath[i].WorldPosition;
                pos.y = 0.5f; // Zvedneme čáru
                _lineRenderer.SetPosition(i + 1, pos);
            }
        }

        private IEnumerator FollowPath(List<GridNode> path)
        {
            IsMoving = true;

            // Vytvoříme si kopii cesty, abychom z ní mohli odebírat body
            // (nechceme měnit původní list, kdyby ho používal někdo jiný)
            List<GridNode> activePath = new List<GridNode>(path);

            // Prvotní vykreslení celé čáry
            UpdatePathVisual(activePath);

            // Procházíme body jeden po druhém
            // Používáme while, protože budeme odebírat z activePath
            while (activePath.Count > 0)
            {
                GridNode targetNode = activePath[0]; // Cíl je vždy první bod v seznamu
                
                Vector3 startPos = transform.position;
                Vector3 endPos = targetNode.WorldPosition;
                endPos.y = 0.5f;

                transform.LookAt(endPos);

                float journeyLength = Vector3.Distance(startPos, endPos);
                float startTime = Time.time;

                // Pohyb k aktuálnímu cíli
                while (Vector3.Distance(transform.position, endPos) > 0.01f)
                {
                    float distCovered = (Time.time - startTime) * _moveSpeed;
                    float fractionOfJourney = distCovered / journeyLength;
                    
                    transform.position = Vector3.Lerp(startPos, endPos, fractionOfJourney);
                    
                    // AKTUALIZACE: Každý frame posuneme začátek čáry na vozík
                    if (_lineRenderer.positionCount > 0)
                    {
                        _lineRenderer.SetPosition(0, transform.position);
                    }

                    yield return null;
                }

                // Jsme v cíli (v aktuálním nodu)
                transform.position = endPos;
                CurrentNode = targetNode;

                // ODEBEREME bod, kterého jsme právě dosáhli, ze seznamu
                activePath.RemoveAt(0);

                // Překreslíme čáru (teď už povede z vozíku rovnou k DALŠÍMU bodu)
                UpdatePathVisual(activePath);
            }

            IsMoving = false;
            _lineRenderer.positionCount = 0; // Vymazat čáru úplně
            Debug.Log("AGV dorazilo do cíle.");
        }
    }
}