using System.Collections;
using UnityEngine;
using Warehouse.Grid;

namespace Warehouse.Units
{
    /// <summary>
    /// Řídí logiku a pohyb jednoho AGV vozíku.
    /// </summary>
    
    public class AGVController : MonoBehaviour
    {
        [Header("Movement Settings")]
        [SerializeField] private float _moveSpeed = 2.0f; // Rychlost pohybu (jednotky za sekundu)

        // Aktuální pozice v logické mřížce
        public GridNode CurrentNode {get; private set;}

        // Příznak, zda se vozík hýbe (pro animace a logiku)
        public bool IsMoving {get; private set;}

        /// <summary>
        /// Inicializace vozíku na startovní pozici.
        /// </summary>
        public void Initialize(GridNode startNode)
        {
            CurrentNode = startNode;
            
            // Nastavíme fyzickou pozici na střed nodu (s korekcí výšky)
            Vector3 startPos = startNode.WorldPosition;
            startPos.y = 0.5f;
            transform.position = startPos;
        }

        /// <summary>
        /// Přesune vozík na cílový uzel plynulým pohybem.
        /// </summary>
        public void MoveToNode(GridNode targetNode)
        {
            if(IsMoving) return;

            StartCoroutine(MoveRoutine(targetNode));
        }

        /// <summary>
        /// Coroutine pro plynulý přesun (interpolaci) mezi dvěma body.
        /// </summary>
        private IEnumerator MoveRoutine(GridNode targetNode)
        {
            IsMoving = true;
            Vector3 startPos = transform.position;
            Vector3 endPos = targetNode.WorldPosition;
            endPos.y = 0.5f;

            // Natočení vozíku směrem k cíli
            transform.LookAt(endPos);

            float journeyLength = Vector3.Distance(startPos, endPos);
            float startTime = Time.time;

            // Smyčka pohybu (dokud nejsme v cíli)
            while(Vector3.Distance(transform.position, endPos) > 0.01f)
            {
                // Vypočítáme, jakou vzdálenost jsme už urazili
                float distCovered = (Time.time - startTime) * _moveSpeed;

                // Jakou část cesty máme za sebou (0.0 až 1.0)
                float fractionOfJourney = distCovered / journeyLength;

                // Lineární interpolace (Lerp)
                transform.position = Vector3.Lerp(startPos, endPos, fractionOfJourney);

                yield return null;
            }

            // Jsme v cíli
            transform.position = endPos;
            CurrentNode = targetNode;
            IsMoving = false;
        }
    }
}