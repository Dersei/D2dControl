# D2dControl

**D2dControl** is a WPF Control for Direct2D with SharpDX. This repository contains code created by dalance and updated to work with .NET Core 3.1.

## Dependencies
- SharpDX ( >= 4.2.0 )

## Usage
Create a class derived from `D2dControl.D2dControl`, and override `Render` method.
In `Render` method, you can use `SharpDX.Direct2D1.RenderTarget` for calling Direct2D functions.
See [sample project](https://github.com/dalance/D2dControl/tree/master/Sample) for details.
