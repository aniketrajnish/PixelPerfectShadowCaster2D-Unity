using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Rendering.Universal;

namespace Makra.Rendering.Tools
{
    /// <summary>
    /// class to hold monobehaviour to attach to shadow-casting sprites to generate pixel-perfect colliders
    /// instructions:
    ///     - attach this script to a shadow-casting sprite
    ///     - make sure the sprite has 'Read/Write Enabled' in texture import settings
    ///     - change the 'Casting Source' of ShadowCaster2D to 'Polygon Collider 2D'
    ///     - change the 'Trim Edges' of ShadowCaster2D to 0
    ///     - set the 'Alpha Threshold' to the desired value, any pixel with alpha < threshold will be trimmed
    ///     - remove the script from the sprite after using it
    /// </summary>
    [ExecuteInEditMode]
    [RequireComponent(typeof(PolygonCollider2D))]
    [RequireComponent(typeof(ShadowCaster2D))]
    [RequireComponent(typeof(SpriteRenderer))]
    public class PixelPerfectShadowCaster2D : MonoBehaviour
    {
        [Range(0f, 1f)]
        public float AlphaThreshold = 0.5f;

        private SpriteRenderer m_spriteRenderer;
        private PolygonCollider2D m_polygonCollider2D;

        void OnEnable()
        {
            m_spriteRenderer = GetComponent<SpriteRenderer>();
            m_polygonCollider2D = GetComponent<PolygonCollider2D>();
            StorePixelPerfectSpritePathInCollider();
        }

        void OnValidate()
        {
            if (Application.isEditor && !Application.isPlaying)
                StorePixelPerfectSpritePathInCollider();
        }
        /// <summary>
        /// store pixel-perfect sprite path in PolygonCollider2D
        /// then ShadowCaster2D will use the collider path to set its shape
        /// </summary>
        void StorePixelPerfectSpritePathInCollider()
        {
            if (m_spriteRenderer.sprite == null)
                return;

            Sprite sprite = m_spriteRenderer.sprite;

            if (sprite.texture == null)
                return;

            if (!sprite.texture.isReadable)
            {
                Debug.LogError("sprite texture not readable, ensure 'Read/Write Enabled' in texture import settings");
                return;
            }
            if (!sprite.texture.alphaIsTransparency)
                Debug.LogWarning("sprite texture 'Alpha Source' isn't 'Input Texture Alpha'");

            Vector2Int[][] pxPaths = PixelPerfectSpritePathCalculator.TraceSprite(sprite, AlphaThreshold);
            if (pxPaths == null || pxPaths.Length <= 0)
            {
                Debug.LogWarning("pixel-perfect silhouette detection found no outline polygons");
                m_polygonCollider2D.enabled = false;
                m_polygonCollider2D.pathCount = 0; // clear paths if no polygons found
                return;
            }
            m_polygonCollider2D.enabled = true;
            m_polygonCollider2D.pathCount = pxPaths.Length;

            float scale = 1.0f / sprite.pixelsPerUnit; // pixels -> meters
            int pathIdx = 0;

            foreach (Vector2Int[] pxPath in pxPaths)
            {
                if (pxPath.Length < 3) // < 3 pts != polygon
                    continue;

                List<Vector2> colliderPtsList = new List<Vector2>();
                foreach (Vector2Int pxPathPt in pxPath)
                {
                    Vector3 localPos = new Vector3(
                        (pxPathPt.x - sprite.pivot.x) * scale,
                        (pxPathPt.y - sprite.pivot.y) * scale,
                        0
                    );
                    colliderPtsList.Add(new Vector2(localPos.x, localPos.y));
                }

                m_polygonCollider2D.SetPath(pathIdx, colliderPtsList.ToArray());
                pathIdx++;
            }

            if (pathIdx < m_polygonCollider2D.pathCount)
                m_polygonCollider2D.pathCount = pathIdx;
        }
    }
}