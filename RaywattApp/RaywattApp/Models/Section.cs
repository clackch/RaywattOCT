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
        private IndicatorBase _mla;

        [ObservableProperty]
        private IndicatorBase _mld;

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

        [ObservableProperty]
        private double _refArea;

        [ObservableProperty]
        private double _refDiameter;

        [ObservableProperty]
        private double _lesionProximal;

        [ObservableProperty]
        private double _lesionDistal;

        [ObservableProperty]
        private double _lesionLengthWidth;

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

            Mla = new IndicatorBase();
            Mla.IsVisible = Visibility.Collapsed;

            Mld = new IndicatorBase();
            Mld.IsVisible = Visibility.Collapsed;

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

        public void CalcMean(List<LumenContour> lumenContours, int frameProximal, int frameDistal)
        {
            if (lumenContours == null || lumenContours.Count == 0)
                return;

            int count = frameDistal - frameProximal + 1;
            List<LumenContour> temp = lumenContours.GetRange(frameProximal, count);

            double meanArea = 0;
            double meanDiameter = 0;

            foreach(LumenContour contour in temp)
            {
                meanArea += contour.Area;
                meanDiameter += contour.MeanDiameter;
            }

            MeanArea = meanArea / temp.Count;
            MeanDiameter = meanDiameter / temp.Count;

            RefArea = (lumenContours[frameProximal].Area + lumenContours[frameDistal].Area) / 2;
            RefDiameter = (lumenContours[frameProximal].MeanDiameter + lumenContours[frameDistal].MeanDiameter) / 2;
        }

        private void SetProximalDisatalArea(double proximalArea, double distalArea)
        {
            Proximal.DValue = proximalArea;
            Distal.DValue = distalArea;
        }

        private void CalcLesionLength(int frameProximal, int frameDistal, int totalFrame, double longitudeWidth, string pullbackLength)
        {
            int frameCnt = int.Parse(CodeDefinition.Codes["PBLE"][pullbackLength]);

            LesionLength.DValue = Math.Round(((frameDistal - frameProximal + 1) * frameCnt / 10) / (double)totalFrame, 1);
            double width = CommonUtil.GetTextBlockSize("TextBlock_Pretendard-Semibold-10", LesionLength.DValue + "㎜", 1).Width + 1;
            LesionLength.X = CommonUtil.GetPositionFromFrame((frameDistal + frameProximal) / 2, totalFrame, longitudeWidth, width / 2);

            LesionLength.IsVisible = Visibility.Visible;

            LesionProximal = CommonUtil.GetPositionFromFrame(frameProximal, totalFrame, longitudeWidth, 0);
            double lesionDistalTemp = CommonUtil.GetPositionFromFrame(frameDistal, totalFrame, longitudeWidth, 0);
            LesionLengthWidth = (lesionDistalTemp - LesionProximal - (width + 10)) / 2;
            LesionDistal = lesionDistalTemp - LesionLengthWidth;
        }

        public bool SetMlaMld(List<LumenContour> lumenContours, int frameProximal, int frameDistal, int totalFrame, double longitudeWidth, string pullbackLength)
        {
            if(lumenContours == null || lumenContours.Count == 0) 
                return false;

            CalcMean(lumenContours, frameProximal, frameDistal);
            SetProximalDisatalArea(lumenContours[frameProximal].Area, lumenContours[frameDistal].Area);
            CalcLesionLength(frameProximal, frameDistal, totalFrame, longitudeWidth, pullbackLength);

            int count = frameDistal - frameProximal + 1;
            double mla = lumenContours.GetRange(frameProximal, count).Min(x => x.Area);
            int mlaIdx = lumenContours.GetRange(frameProximal, count).FindIndex(x => x.Area == mla);

            double mld = lumenContours.GetRange(frameProximal, count).Min(x => x.MeanDiameter);
            int mldIdx = lumenContours.GetRange(frameProximal, count).FindIndex(x => x.MeanDiameter == mld);

            if(mlaIdx <= mldIdx)
            {
                Mla.X = CommonUtil.GetPositionFromFrame(mlaIdx + frameProximal, totalFrame, longitudeWidth, Constants.SectionValueWidth - Constants.SectionValueCenterWidth);
                Mld.X = CommonUtil.GetPositionFromFrame(mldIdx + frameProximal, totalFrame, longitudeWidth, -Constants.SectionValueCenterWidth);
                Mla.StrValue = "Left";
                Mld.StrValue = "Right";
                MlaValue.DValue = mla;
                MlaValue.NValue = mlaIdx + frameProximal;
                string text = "MLA " + Math.Round(mla * Constants.MillimeterPerPixel * Constants.MillimeterPerPixel, 2).ToString() + "㎟";
                double width = CommonUtil.GetTextBlockSize("TextBlock_Pretendard-Semibold-12", text, 2).Width;
                MlaValue.X = Mla.X - (width + 18);
                MldValue.DValue = lumenContours[mldIdx + frameProximal].MeanDiameter;
                MldValue.X = Mld.X + Constants.SectionValueWidth;
            }
            else
            {
                Mla.X = CommonUtil.GetPositionFromFrame(mlaIdx + frameProximal, totalFrame, longitudeWidth,  - Constants.SectionValueCenterWidth);
                Mld.X = CommonUtil.GetPositionFromFrame(mldIdx + frameProximal, totalFrame, longitudeWidth, Constants.SectionValueWidth - Constants.SectionValueCenterWidth);
                Mla.StrValue = "Right";
                Mld.StrValue = "Left";
                MlaValue.DValue = mla;
                MlaValue.NValue = mlaIdx + frameProximal;
                MlaValue.X = Mla.X + Constants.SectionValueWidth;
                MldValue.DValue = lumenContours[mldIdx + frameProximal].MeanDiameter;
                string text = "MLD " + Math.Round(mla * Constants.MillimeterPerPixel * Constants.MillimeterPerPixel, 2).ToString() + "㎜";
                double width = CommonUtil.GetTextBlockSize("TextBlock_Pretendard-Semibold-12", text, 2).Width;              
                MldValue.X = Mld.X - (width + 18);
            }

            return true;
        }

        public bool SetMsaMinExp(List<LumenContour> lumenContours, int frameProximal, int frameDistal, int stentProximal, int stentDistal, int totalFrame, double longitudeWidth, string pullbackLength)
        {
            if (lumenContours == null || lumenContours.Count == 0)
                return false;

            CalcMean(lumenContours, frameProximal, frameDistal);
            SetProximalDisatalArea(lumenContours[frameProximal].Area, lumenContours[frameDistal].Area);
            CalcLesionLength(frameProximal, frameDistal, totalFrame, longitudeWidth, pullbackLength);

            int tempProximal = frameProximal > stentProximal ? frameProximal : stentProximal;
            int tempDistal = frameDistal < stentDistal ? frameDistal : stentDistal;

            int count = tempDistal - tempProximal + 1;
            if (count <= 0)
                return false;

            double msa = lumenContours.GetRange(tempProximal, count).Min(x => x.Area);
            int msaIdx = lumenContours.GetRange(tempProximal, count).FindIndex(x => x.Area == msa);

            //TODO) Min Exp 정의가 되면 Min Exp 변경 필요
            double minExp = msa;
            int minExpIdx = msaIdx;

            if(msaIdx <= minExpIdx)
            {
                Msa.X = CommonUtil.GetPositionFromFrame(msaIdx + tempProximal, totalFrame, longitudeWidth, Constants.SectionValueWidth - Constants.SectionValueCenterWidth);
                MinExp.X = CommonUtil.GetPositionFromFrame(minExpIdx + tempProximal, totalFrame, longitudeWidth, - Constants.SectionValueCenterWidth);
                Msa.StrValue = "Left";
                MinExp.StrValue = "Right";
                MsaValue.DValue = msa;
                MsaValue.NValue = msaIdx + tempProximal;
                string text = "MSA " + Math.Round(msa * Constants.MillimeterPerPixel  * Constants.MillimeterPerPixel , 2).ToString() + "㎟";
                double width = CommonUtil.GetTextBlockSize("TextBlock_Pretendard-Semibold-12", text, 2).Width;
                MsaValue.X = Msa.X - (width + 18);
                MinExpValue.DValue = minExp;
                MinExpValue.X = MinExp.X + Constants.SectionValueWidth;
            }
            else
            {
                Msa.X = CommonUtil.GetPositionFromFrame(msaIdx + tempProximal, totalFrame, longitudeWidth, - Constants.SectionValueCenterWidth);
                MinExp.X = CommonUtil.GetPositionFromFrame(minExpIdx + tempProximal, totalFrame, longitudeWidth, Constants.SectionValueWidth - Constants.SectionValueCenterWidth);
                Msa.StrValue = "Right";
                MinExp.StrValue = "Left";
                MsaValue.DValue = msa;
                MsaValue.NValue = msaIdx + tempProximal;
                MsaValue.X = Msa.X + Constants.SectionValueWidth;
                MinExpValue.DValue = minExp;
                string text = "Min Exp. " + minExp + "%";
                double width = CommonUtil.GetTextBlockSize("TextBlock_Pretendard-Semibold-12", text, 2).Width;
                MinExpValue.X = MinExp.X - (width + 18);
            }

            return true;
        }

        public void VisibleMlaMld(bool isVisible)
        {   
            Visibility visibility = isVisible ? Visibility.Visible : Visibility.Collapsed;

            Mla.IsVisible = visibility;
            Mld.IsVisible = visibility;
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
