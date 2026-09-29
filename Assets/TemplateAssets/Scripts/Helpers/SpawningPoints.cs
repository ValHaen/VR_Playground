using TMPro;
using Unity.Netcode;
using UnityEngine;

namespace XRMultiplayer
{
    public class SpawningPointsHelper : MonoBehaviour
    {
        [SerializeField] public int m_maxPlayerCount;
        [SerializeField] float radius = 2.0f;
        [SerializeField] GameObject spawnPointPrefab;

        void Start()
        {
            GenerateSpawnPoints();
        }

        /// <summary>
        /// Generates spawning points in set radius around spawnPointPrefab
        /// </summary>
        public void GenerateSpawnPoints()
        {
            // Use m_maxPlayerCount to generate number of spawn points
            for (int i = 0; i < m_maxPlayerCount; i++)
            {
                float angle = i * (360f / m_maxPlayerCount);
                float rad = angle * Mathf.Deg2Rad;
                Vector3 localPosition = new Vector3(
                    Mathf.Sin(rad) * radius,
                    0,
                    Mathf.Cos(rad) * radius
                );


                // Instantiate spawn points using spawnPointPrefab and set position, rotation and number to display
                GameObject spawnPoint = Instantiate(spawnPointPrefab, transform.position, Quaternion.identity, transform);

                spawnPoint.transform.localPosition = localPosition;
                spawnPoint.transform.GetComponentInChildren<TextMeshProUGUI>().text = (i + 1).ToString();
                spawnPoint.transform.LookAt(spawnPointPrefab.transform.position);

                spawnPoint.name = $"SpawnPoint_{i + 1}";
            }

            spawnPointPrefab.SetActive(false);
        }
    }
}