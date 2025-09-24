using TMPro;
using UnityEngine;
using static DaftAppleGames.SeaTruckRecall_BZ.SeaTruckDockRecallPlugin;

namespace DaftAppleGames.SeaTruckRecall_BZ.Navigation
{
    internal enum CellType { Start, End, NavCell, Route }

    internal class CellVisualiser : MonoBehaviour
    {
        [SerializeField] private TMP_Text label;
        [SerializeField] private NavCell navCell;
        [SerializeField] private CellType cellType;
        [SerializeField] private GameObject visualiserSphere;

        internal void CreateOrUpdate(NavCell newNavCell, CellType newCellType, Transform parentContainer)
        {
            cellType = newCellType;
            navCell = newNavCell;
            CreateOrUpdateVisualiserSphere();
            transform.position = newNavCell.Position;
            if (parentContainer)
            {
                gameObject.transform.SetParent(parentContainer, true);
            }

            label.text = newNavCell.Name;
        }

        private void CreateOrUpdateVisualiserSphere()
        {
            CreateOrderUpdateVisualiserSphere(visualiserSphere);
        }

        private void CreateOrderUpdateVisualiserSphere(GameObject sphere)
        {
            switch (cellType)
            {
                case CellType.Start:
                    visualiserSphere = CreateOrUpdateSphere(sphere,0.5f, Color.yellow);
                    break;
                case CellType.End:
                    visualiserSphere = CreateOrUpdateSphere(sphere,0.5f, Color.blue);
                    break;
                case CellType.NavCell:
                    visualiserSphere = CreateOrUpdateSphere(sphere,0.5f, navCell.HasColliders ? Color.red : Color.green);
                    break;
                case CellType.Route:
                    visualiserSphere = CreateOrUpdateSphere(sphere,0.5f, Color.white);
                    break;
                default:
                    ModDebugLog.LogDebug("CellVisualiser: Unknown CellType");
                    break;
            }
        }

        private GameObject CreateOrUpdateSphere(GameObject sphere, float radius, Color color)
        {
            if (!sphere)
            {
                sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            }

            if (sphere.GetComponent<Collider>())
            {
                Destroy(sphere.GetComponent<Collider>());                
            }
            sphere.transform.SetParent(gameObject.transform, false);
            sphere.transform.localPosition = Vector3.zero;
            sphere.transform.localScale = new Vector3(radius, radius, radius);
            sphere.GetComponent<Renderer>().material.color = color;

            return sphere;
        }
    }
}