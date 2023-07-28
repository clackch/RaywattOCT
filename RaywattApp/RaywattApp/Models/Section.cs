using CommunityToolkit.Mvvm.ComponentModel;
using log4net;
using RaywattApp.Common.Annotation.Models;
using RaywattApp.Common.Bases;
using RaywattApp.Common.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;

namespace RaywattApp.Models
{
    public partial class Section : ObservableObject
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(Section));

        [ObservableProperty]
        private Indicator _proximal;

        [ObservableProperty]
        private Indicator _distal;

        [ObservableProperty]
        private IndicatorBase _mlaMld;

        [ObservableProperty]
        private IndicatorBase _mlaValue;

        [ObservableProperty]
        private IndicatorBase _mldValue;

        [ObservableProperty]
        private IndicatorBase _msa;

        [ObservableProperty]
        private IndicatorBase _msaValue;

        [ObservableProperty]
        private IndicatorBase _minExp;

        [ObservableProperty]
        private IndicatorBase _minExpValue;

        [ObservableProperty]
        private IndicatorBase _lesionLength;

        [ObservableProperty]
        private double _meanArea;

        [ObservableProperty]
        private double _meanDiameter;

        public Section()
        {
            Proximal = new Indicator();
            Proximal.IsSectionIndicator = true;
            Proximal.IsSectionProximal = true;
            Proximal.IsVisible = Visibility.Collapsed;
            Proximal.IsEnabled = false;

            Distal = new Indicator();
            Distal.IsSectionIndicator = true;
            Distal.IsSectionProximal = false;
            Distal.IsVisible = Visibility.Collapsed;
            Distal.IsEnabled = false;

            MlaMld = new IndicatorBase();
            MlaMld.IsVisible = Visibility.Collapsed;
            MlaMld.X = 0 - Constants.SectionMlaMldWidth / 2;

            MlaValue = new IndicatorBase();
            MlaValue.IsVisible = Visibility.Collapsed;

            MldValue = new IndicatorBase();
            MldValue.IsVisible = Visibility.Collapsed;

            Msa = new IndicatorBase();
            Msa.IsVisible = Visibility.Collapsed;

            MinExp = new IndicatorBase();
            MinExp.IsVisible = Visibility.Collapsed;

            MsaValue = new IndicatorBase();
            MsaValue.IsVisible = Visibility.Collapsed;

            MinExpValue = new IndicatorBase();
            MinExpValue.IsVisible = Visibility.Collapsed;

            LesionLength = new IndicatorBase();
            LesionLength.IsVisible = Visibility.Collapsed;
        }

        private void CalcMean(List<LumenContour> LumenContours, int frameProximal, int frameDistal)
        {
            int count = frameDistal - frameProximal + 1;
            List<LumenContour> temp = LumenContours.GetRange(frameProximal, count);

            double meanArea = 0;
            double meanDiameter = 0;

            foreach(LumenContour contour in temp)
            {
                meanArea += contour.Area;
                meanDiameter += contour.MeanDiameter;
            }

            MeanArea = meanArea / temp.Count;
            MeanDiameter = meanDiameter / temp.Count;
        }

        private void SetProximalDisatalArea(double proximalArea, double distalArea)
        {
            Proximal.DValue = proximalArea;
            Distal.DValue = distalArea;
        }

        private void CalcLesionLength(int frameProximal, int frameDistal, int totalFrame, double longitudeWidth, string pullbackType)
        {
            int frameCnt = pullbackType == Constants.PullbackTypeLong ? Constants.PullbackLongFrameCnt : Constants.PullbackShortFrameCnt;

            LesionLength.DValue = Math.Round(((frameDistal - frameProximal + 1) * frameCnt / 10) / (double)totalFrame, 1);
            double width = CommonUtil.GetTextBlockSize("TextBlock_Pretendard-Semibold-12", LesionLength.DValue + "㎜").Width;
            LesionLength.X = CommonUtil.GetPositionFromFrame((frameDistal + frameProximal) / 2, totalFrame, longitudeWidth, width / 2);

            LesionLength.IsVisible = Visibility.Visible;
        }

        public void SetMlaMld(List<LumenContour> LumenContours, int frameProximal, int frameDistal, int totalFrame, double longitudeWidth, string pullbackType)
        {
            CalcMean(LumenContours, frameProximal, frameDistal);
            SetProximalDisatalArea(LumenContours[frameProximal].Area, LumenContours[frameDistal].Area);
            CalcLesionLength(frameProximal, frameDistal, totalFrame, longitudeWidth, pullbackType);

            int count = frameDistal - frameProximal + 1;
            double mla = LumenContours.GetRange(frameProximal, count).Min(x => x.Area);
            int mlaIdx = LumenContours.GetRange(frameProximal, count).FindIndex(x => x.Area == mla);

            //Diameter 로직에 따라 mla, mld 분리 여부 결정
            double mld = LumenContours.GetRange(frameProximal, count).Min(x => x.MeanDiameter);
            int mldIdx = LumenContours.GetRange(frameProximal, count).FindIndex(x => x.MeanDiameter == mld);

            MlaMld.X = CommonUtil.GetPositionFromFrame(mlaIdx + frameProximal, totalFrame, longitudeWidth, Constants.SectionMlaMldWidth / 2);
            MlaValue.DValue = mla;
            MlaValue.NValue = mlaIdx + frameProximal;
            string text = "MLA " + Math.Round(mla * Constants.MilimeterPerPixel * Constants.MilimeterPerPixel, 2).ToString() + "㎟";
            double width = CommonUtil.GetTextBlockSize("TextBlock_Pretendard-Semibold-12", text).Width;
            MlaValue.X = MlaMld.X - (width + 2);
            MldValue.DValue = LumenContours[mlaIdx + frameProximal].MeanDiameter;
            MldValue.X = MlaMld.X + Constants.SectionMlaMldWidth + 2;
        }

        public void SetMsaMinExp(List<LumenContour> LumenContours, int frameProximal, int frameDistal, int totalFrame, double longitudeWidth, string pullbackType)
        {
            CalcMean(LumenContours, frameProximal, frameDistal);
            SetProximalDisatalArea(LumenContours[frameProximal].Area, LumenContours[frameDistal].Area);
            CalcLesionLength(frameProximal, frameDistal, totalFrame, longitudeWidth, pullbackType);

            int count = frameDistal - frameProximal + 1;
            double msa = LumenContours.GetRange(frameProximal, count).Min(x => x.Area);
            int msaIdx = LumenContours.GetRange(frameProximal, count).FindIndex(x => x.Area == msa);

            //Min Exp 정의가 되면 Min Exp 변경 필요 (Test로 Max로 설정)
            double minExp = LumenContours.GetRange(frameProximal, count).Max(x => x.Area);
            int minExpIdx = LumenContours.GetRange(frameProximal, count).FindIndex(x => x.Area == minExp);
            minExp = Math.Round(minExp / 10000, 0);

            if(msaIdx <= minExpIdx)
            {
                Msa.X = CommonUtil.GetPositionFromFrame(msaIdx + frameProximal, totalFrame, longitudeWidth, Constants.SectionMsaMinExpWidth);
                MinExp.X = CommonUtil.GetPositionFromFrame(minExpIdx + frameProximal, totalFrame, longitudeWidth, 4);
                Msa.StrValue = "Left";
                MinExp.StrValue = "Right";
                MsaValue.DValue = msa;
                MsaValue.NValue = msaIdx + frameProximal;
                string text = "MSA " + Math.Round(msa * Constants.MilimeterPerPixel * Constants.MilimeterPerPixel, 2).ToString() + "㎟";
                double width = CommonUtil.GetTextBlockSize("TextBlock_Pretendard-Semibold-12", text).Width;
                MsaValue.X = Msa.X - (width + 2);
                MinExpValue.DValue = minExp;
                MinExpValue.X = MinExp.X + Constants.SectionMsaMinExpWidth + 4 + 2;
            }
            else
            {
                Msa.X = CommonUtil.GetPositionFromFrame(msaIdx + frameProximal, totalFrame, longitudeWidth, 4);
                MinExp.X = CommonUtil.GetPositionFromFrame(minExpIdx + frameProximal, totalFrame, longitudeWidth, Constants.SectionMsaMinExpWidth);
                Msa.StrValue = "Right";
                MinExp.StrValue = "Left";
                MsaValue.DValue = msa;
                MsaValue.NValue = msaIdx + frameProximal;
                MsaValue.X = Msa.X + Constants.SectionMsaMinExpWidth + 4 + 2;
                MinExpValue.DValue = minExp;
                string text = "Min Exp. " + minExp + "%";
                double width = CommonUtil.GetTextBlockSize("TextBlock_Pretendard-Semibold-12", text).Width;
                MinExpValue.X = MinExp.X - (width + 2);
            }            
        }

        public void VisibleMlaMld(bool isVisible)
        {   
            Visibility visibility = isVisible ? Visibility.Visible : Visibility.Collapsed;
            
            MlaMld.IsVisible = visibility;
            MlaValue.IsVisible = visibility;
            MldValue.IsVisible = visibility;
        }

        public void VislbleMsaMinExp(bool isVisible)
        {
            Visibility visibility = isVisible ? Visibility.Visible : Visibility.Collapsed;

            Msa.IsVisible = visibility;
            MinExp.IsVisible = visibility;
            MsaValue.IsVisible = visibility;
            MinExpValue.IsVisible = visibility;
        }
    }
}
