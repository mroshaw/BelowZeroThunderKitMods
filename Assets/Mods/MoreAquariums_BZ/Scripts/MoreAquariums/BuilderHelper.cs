using System.Collections.Generic;
using UnityEngine;

namespace DaftAppleGames.MoreAquariums
{
	public class BuilderHelper : MonoBehaviour
	{
		[SerializeField] private GameObject prefab;
		[SerializeField] private List<OrientedBounds> bounds;
		[SerializeField] private List<Collider> colliders;
		[SerializeField] private List<Renderer> renderers;
		[SerializeField] private GameObject ghostModel;
		[SerializeField] private bool isPlacing;

		[SerializeField] private List<GameObject> obstacles;
		[SerializeField] private List<GameObject> overlappingColliders;
		
		private void Refresh()
		{
			prefab = Builder.prefab;
			bounds = Builder.bounds;
			colliders = Builder.sCollidersList;
			renderers = Builder.sRenderers;
			ghostModel = Builder.ghostModel;
			isPlacing = Builder.isPlacing;
		}

		private void RefreshBlockers()
		{
			obstacles = new List<GameObject>();
			overlappingColliders = new List<GameObject>();
			
			Builder.GetObstacles(Builder.placePosition, Builder.placeRotation, Builder.bounds, null, obstacles);
			bool result = Builder.CheckAsSubModule(out var coll);
			Builder.CheckSpace(Builder.placePosition, Builder.placeRotation, Builder.bounds, Builder.placeLayerMask.value, coll, overlappingColliders);
		}
	}
}