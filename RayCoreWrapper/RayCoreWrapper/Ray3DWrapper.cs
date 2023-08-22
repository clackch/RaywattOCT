using System.Runtime.InteropServices;

namespace RaywattOCT
{
    public class Ray3DWrapper
    {
        public class Ray3DStatus
        {
            private Ray3DObjectMode[] _objectVisibility = new Ray3DObjectMode[(int)Ray3DObject.Count];
            public Ray3DObjectMode[] ObjectVisibility
            {
                get { return _objectVisibility; }
                set { _objectVisibility = value; }
            }

            private bool _cutviewOn = true;
            public bool CutViewOn
            {
                get { return _cutviewOn; }
                set { _cutviewOn = value; }
            }

            public Ray3DStatus()
            {
                for (Ray3DObject obj = Ray3DObject.Tissue; obj < Ray3DObject.Count; obj++) 
                {
                    ObjectVisibility[(int)obj] = Ray3DObjectMode.Hide;
                }
                ObjectVisibility[(int)Ray3DObject.Tissue] = Ray3DObjectMode.Cut;
                CutViewOn = true;
            }

            public int ShowObject(Ray3DObject obj, Ray3DObjectMode mode)
            {
                ObjectVisibility[(int)obj] = mode;
                return ODSOCT_SetViewData(obj, mode);
            }

            public bool IsObjectVisible(Ray3DObject obj)
            {
                return (ObjectVisibility[(int)obj] != Ray3DObjectMode.Hide);
            }
        }

        private static Ray3DStatus _ray3DStatus;
        public static Ray3DStatus ray3DStatus
        { 
            get { return _ray3DStatus; }
            set { _ray3DStatus = value; }
        }

        public static int MinWaitingDelay = 50;
        public enum Ray3DObject : int
        {
            Unknown = 0,
            Tissue,
            Lumen,
            Stent,
            StentMalaposition,
            GuideWire,
            GuideWire2,
            SideBranch,
            Count
        };
        public enum Ray3DObjectMode : int 
        { 
            Hide,
            Cut,
            Full
        };
        public enum Ray3DMode : int 
        { 
            Unknown = 0,
            Render,
            Distance,
            Area,
            FindBranch
        };
        public enum Ray3DViewID : int 
        { 
            CutView = 0,
            FlyThrough
        };

        [DllImport("OCT3d.dll")]
        public static extern int ODSOCT_CreateDll(IntPtr hWnd);
        [DllImport("OCT3d.dll")]
        public static extern int ODSOCT_CreateOCTWindowByPos(Ray3DViewID id, int x, int y, int width, int height);
        [DllImport("OCT3d.dll")]
        public static extern int ODSOCT_ShowAllWindows();
        [DllImport("OCT3d.dll")]
        public static extern int ODSOCT_HideAllWindows();
        [DllImport("OCT3d.dll")]
        public static extern int ODSOCT_StartRendering();
        [DllImport("OCT3d.dll")]
        public static extern int ODSOCT_EnableInteractor(Ray3DViewID id, bool enable);
        [DllImport("OCT3d.dll")]
        public static extern int ODSOCT_EnableWheelEvent(bool enable);
        [DllImport("OCT3d.dll")]
        public static extern int ODSOCT_DeleteDll();
        [DllImport("OCT3d.dll")]
        public static extern int ODSOCT_InputData(Ray3DObject obj, IntPtr raw, int width, int height, int depth, double scaleX, double scaleY, double scaleZ);
        [DllImport("OCT3d.dll")]
        public static extern int ODSOCT_ProcessingDatas();
        [DllImport("OCT3d.dll")]
        public static extern int ODSOCT_InputSurfaceParameter(Ray3DObject obj, int smooth, int threshold, string textureFilePath);
        [DllImport("OCT3d.dll")]
        public static extern int ODSOCT_SetViewData(Ray3DObject obj, Ray3DObjectMode mode);
        [DllImport("OCT3d.dll")]
        public static extern int ODSOCT_MoveCameraPosition(int direction, bool inverse);
        [DllImport("OCT3d.dll")]
        public static extern int ODSOCT_MoveToFrame(int frame);
        [DllImport("OCT3d.dll")]
        public static extern int ODSOCT_SetFov(int angle);
        [DllImport("OCT3d.dll")]
        public static extern int ODSOCT_Rendering();
        [DllImport("OCT3d.dll")]
        public static extern int ODSOCT_RenderingForce();
        [DllImport("OCT3d.dll")]
        public static extern int ODSOCT_MoveSliceAngle(int direction);
        [DllImport("OCT3d.dll")]
        public static extern int ODSOCT_RotateAngle(float angle);
        [DllImport("OCT3d.dll")]
        public static extern int ODSOCT_CutViewZoom(int zoomDirection);
        [DllImport("OCT3d.dll")]
        public static extern int ODSOCT_Set3DMode(Ray3DMode mode);
        [DllImport("OCT3d.dll")]
        public static extern int ODSOCT_ShowIndicatorCutView(bool show);
        [DllImport("OCT3d.dll")]
        public static extern int ODSOCT_ShowCuttingline(bool show);
    }
}
