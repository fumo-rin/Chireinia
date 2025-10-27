using UnityEngine;

namespace FumoCore.Tools
{
    public static class Collider2DExtension
    {
        public static bool TryBoxcastBottom(this BoxCollider2D collider, float size, LayerMask layerMask, out RaycastHit2D hit)
        {
            hit = default(RaycastHit2D);
            if (collider == null) return false;
            Vector2 worldCenter = collider.transform.TransformPoint(collider.offset);
            Vector2 worldSize = Vector2.Scale(collider.size, collider.transform.lossyScale);
            Vector2 origin = worldCenter + Vector2.down * (worldSize.y / 2f);
            hit = Physics2D.BoxCast(
                origin,
                worldSize,
                collider.transform.eulerAngles.z,
                Vector2.down,
                size,
                layerMask
            );
            return hit.collider != null;
        }
    }
}
