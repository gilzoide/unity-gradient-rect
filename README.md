# Gradient Rect
[![openupm](https://img.shields.io/npm/v/com.gilzoide.gradient-rect?label=openupm&registry_uri=https://package.openupm.com)](https://openupm.com/packages/com.gilzoide.gradient-rect/)

Unity UI components with color [Gradients](https://docs.unity3d.com/ScriptReference/Gradient.html) embedded in their meshes.

![](Extras~/demo.gif)


## Features
- Uses Unity's built-in Gradient class.
- Supports additional color tint, 2D textures and sprites.
- Supports custom materials and masking.
  Gradient colors are embedded in the generated meshes, the components will never mess with your materials.
- Supports mesh modifiers, like Outline and Shadow.


## How to install
Either:

- Install using [openupm](https://openupm.com/):
  ```
  openupm add com.gilzoide.gradient-rect
  ```

- Install via [Unity Package Manager](https://docs.unity3d.com/Manual/upm-ui-giturl.html) using the following URL:
  ```
  https://github.com/gilzoide/unity-gradient-rect.git#1.1.0
  ```


## Components

### Gradient Effect

[Gradient Effect](Runtime/GradientEffect.cs) adds a gradient to a Unity UI `Graphic`, including `Image` and `RawImage`. Add it via **Add Component → UI → Effects → Gradient Effect**, then set **Gradient** and **Angle**.

- **Angle** accepts any angle in degrees. `0°` runs left to right, `90°` bottom to top, `180°` right to left, and `270°` top to bottom.
- On an **Image**, Preserve Aspect fits the gradient to the sprite, and Fill Amount clips it in place. Use the Image inspector for slicing and tiling.
- On a **Raw Image**, the gradient spans the UI rect independently of **Texture** and **UV Rect**.
- The effect multiplies the existing tint and uses the Graphic's material. Multiple effects on the same Graphic apply in component order.
- In Linear color space, colors are sampled in the same space Unity uses to interpolate UI vertices.
- If subdivision would exceed uGUI's vertex limit, the effect samples the gradient on the existing vertices.

### Legacy components

- [Gradient Rect (Legacy)](Runtime/GradientRect.cs): rectangle colored with Gradient colors.
- [Gradient Texture (Legacy)](Runtime/GradientTexture.cs): texture with configurable UV, tinted with Gradient colors.
- [Gradient Image (Legacy)](Runtime/GradientImage.cs): sprite tinted with Gradient colors.
  UVs are automatically fetched from sprite data.
  Only simple filling is supported, no slicing nor tiling.
