# PixelPerfectShadowCaster2D-Unity
Pixel Perfect Shadow Caster (2D) for Unity using Contour Tracing!

Comparison between the following shape providers:
- `SpriteRenderer` that uses `Sprite.vertices`
- `PolygonCollider2D` that uses `Sprite.GetPhysicsShape`
- `PixelPerfectShadowCaster2D` that uses contour tracing (ours)
  
![comparision](https://github.com/user-attachments/assets/6381d40f-2a91-44b7-bf5f-2922bb0c8e7b)

## How To Use
- Attach this script to a shadow-casting sprite
- Make sure the sprite has `Read/Write Enabled` in texture import settings
- Change the `Casting Source` of `ShadowCaster2D` to `Polygon Collider 2D`
- Change the `Trim Edges` of `ShadowCaster2D` to 0
- Set the `Alpha Threshold` to the desired value, any pixel with alpha < threshold will be trimmed
- Remove the script from the sprite after using it

> [!NOTE]
> Using the `PolygonCollider2D` as a mediator to set the path of the `ShadowCaster2D` instead of directly applying it as Unity recommends leaving the code within `UnityEngine.Rendering.Universal` untouched!

https://github.com/user-attachments/assets/458180ca-3552-439e-a8fa-b76e4b22a0b3

## Contributing
Contributions to the project are welcome.

## License
MIT

## Acknowledgement
Thankful to [RandomiaGaming](https://github.com/RandomiaGaming) for Contour Tracing Algorithm.
