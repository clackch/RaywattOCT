using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace RayCoreWrapper
{
    public class Ray3DWrapper
    {
        public enum Ray3DObject : int
        {
            Unknown = 0,
            Tissue,
            Lumen,
            Stent,
            StentMalaposition,
            GuideWire,
            GuideWire2,
            SideBranch
        };
        public enum Ray3DRenderMode : int
        {
            Unknown = 0,
            Tissue,
            Lumen,
            Stent,
            StentMalaposition,
            GuideWire,
            GuideWire2,
            SideBranch
        };
        public enum Ray3DActionMode : int 
        { 
            Unknown = 0,
            Render,
            Distance,
            Area,
            Spline
        };
        public enum Ray3DWindowType : int 
        { 
            CutView = 0,
            FlyThrough,
            Longitude,
            CrossSection
        };

        [DllImport("OCT3d.dll")]
        public static extern int ODSOCT_CreateDll();
        [DllImport("OCT3d.dll")]
        public static extern int ODSOCT_DeleteDll();
        [DllImport("OCT3d.dll")]
        public static extern int ODSOCT_InputData(Ray3DObject obj, IntPtr raw, int width, int height, int depth, double scaleX, double scaleY, double scaleZ);
        [DllImport("OCT3d.dll")]
        public static extern int ODSOCT_ProcessingDatas();
        [DllImport("OCT3d.dll")]
        public static extern int ODSOCT_InputSurfaceParameter(Ray3DObject obj, int smooth, int threshold, string textureFilePath);
        [DllImport("OCT3d.dll")]
        public static extern int ODSOCT_SetViewData(Ray3DObject obj, Ray3DRenderMode mode);
        [DllImport("OCT3d.dll")]
        public static extern int ODSOCT_MoveCameraPosition(int direction, bool inverse);
        [DllImport("OCT3d.dll")]
        public static extern int ODSOCT_SetFov(int angle);
        [DllImport("OCT3d.dll")]
        public static extern int ODSOCT_Rendering();
        [DllImport("OCT3d.dll")]
        public static extern int ODSOCT_RenderingForce();
        [DllImport("OCT3d.dll")]
        public static extern int ODSOCT_MoveSliceAngle(int direction);
        [DllImport("OCT3d.dll")]
        public static extern int ODSOCT_CutViewZoom(int zoomDirection);
        [DllImport("OCT3d.dll")]
        public static extern int ODSOCT_Set3DMode(Ray3DActionMode mode);
        [DllImport("OCT3d.dll")]
        public static extern int ODSOCT_ShowIndicatorCutView(bool show);
        [DllImport("OCT3d.dll")]
        public static extern int ODSOCT_Show2dView(bool show);
        [DllImport("OCT3d.dll")]
        public static extern int ODSOCT_ShowCuttingline(bool show);
        [DllImport("OCT3d.dll")]
        public static extern int ODSOCT_CreateOCTWindowByPos(IntPtr hWnd, Ray3DWindowType type, int x, int y, int width, int height);
    }
}
