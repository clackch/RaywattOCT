using RaywattApp.Common.Annotation.Models;
using RaywattApp.Common.Bases;
using RaywattApp.Common.Util;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace RaywattApp.Common.Annotation
{
    public partial class DrawUtil
    {
        private const string constText = "Text";

        private const string constPointer = "Pointer";

        private const string constTextLine = "TextLine";

        private const string constTextBox = "TextBox";

        private List<TextGeometry> textGeometries;

        private Point pointerPoint;

        private Point textPoint;

        private bool isPointerClicked;

        private bool isTextClicked;

        private bool isDoubleClicked;

        private double diffX;

        private double diffY;

        private void TextInit()
        {
            this.isPointerClicked = false;
            this.isTextClicked = false;
            this.isDoubleClicked = false;
            this.diffX = 0.0;
            this.diffY = 0.0;
        }

        //---------------------------------------------------------------------------------------------------- Event
        private void text_canvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            _log.Debug("text_canvas_MouseLeftButtonDown");

            if (this.isDrawing)
            {
                this.pointerPoint = e.GetPosition(this.canvas);
                this.textPoint = e.GetPosition(this.canvas);
                DrawPointer(this.pointerPoint, this.textGeometries.Count);

                double labelHeight = GetLabelSize("StyleLabelText").Height;
                double textBoxWidth = GetTextBoxSize("StyleTextBox").Width;

                if (this.canvas.ActualHeight - this.textPoint.Y < labelHeight)
                {
                    this.textPoint.Y = this.canvas.ActualHeight - labelHeight;
                }
                if (this.canvas.ActualWidth - this.textPoint.X < textBoxWidth)
                {
                    this.textPoint.X = this.canvas.ActualWidth - textBoxWidth;
                }

                InCommand = Constants.MeasureDisableText;
                CommandOff = true;

                DrawTextInput(this.textPoint, this.textGeometries.Count, "");

                TextGeometry textGeometry = new TextGeometry();
                textGeometry.PointerPoint = this.pointerPoint;
                textGeometry.TextPoint = this.textPoint;
                textGeometry.Group = this.textGeometries.Count;
                this.textGeometries.Add(textGeometry);
            }
        }

        private void text_canvas_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            _log.Debug("text_canvas_MouseRightButtonDown");

            InCommand = Constants.MeasureDisableText;
            CommandOff = true;
        }

        private void pointer_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            _log.Debug("pointer_MouseLeftButtonDown");

            if (this.isDrawing)
                return;

            if (this.isErasing)
            {
                Ellipse ellipse = sender as Ellipse;
                string[] tempArr = ellipse.Name.Split('_');
                int group = int.Parse(tempArr[1]);

                DeleteTextAll();

                for (int i = group + 1; i < this.textGeometries.Count; i++)
                {
                    this.textGeometries[i].Group--;
                }
                this.textGeometries.RemoveAt(group);

                DrawTextAll();

                return;
            }

            this.isPointerClicked = true;
            Mouse.Capture((FrameworkElement)sender);
        }

        private void pointer_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            _log.Debug("pointer_MouseLeftButtonUp");

            if (this.isPointerClicked)
            {
                this.isPointerClicked = false;

                Ellipse ellipse = sender as Ellipse;
                string[] tempArr = ellipse.Name.Split('_');
                int group = int.Parse(tempArr[1]);

                DeletePointer(group);
                DrawPointer(this.textGeometries[group].PointerPoint, group);

                DeleteLabel(constText, group);
                DrawText(this.textGeometries[group].TextPoint, group, this.textGeometries[group].Text);

                Mouse.Capture(null);
            }
        }

        private void pointer_MouseMove(object sender, MouseEventArgs e)
        {
            if (this.isPointerClicked)
            {
                Ellipse ellipse = sender as Ellipse;
                Point point = e.GetPosition(this.canvas);

                string[] tempArr = ellipse.Name.Split('_');
                int group = int.Parse(tempArr[1]);

                TextGeometry textGeometry = this.textGeometries[group];

                if (point.X > this.canvas.ActualWidth - ellipse.Width / 2 || point.X < ellipse.Width / 2)
                    point.X = textGeometry.PointerPoint.X;

                if (point.Y > this.canvas.ActualHeight - ellipse.Height / 2 || point.Y < ellipse.Height / 2)
                    point.Y = textGeometry.PointerPoint.Y;

                textGeometry.PointerPoint = point;

                Canvas.SetLeft(ellipse, point.X);
                Canvas.SetTop(ellipse, point.Y);

                DeleteTextLine(this.textGeometries[group].Group);
                DrawTextLine(this.textGeometries[group].PointerPoint, this.textGeometries[group].TextPoint, this.textGeometries[group].Group);
            }
        }

        private void pointer_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            _log.Debug("pointer_MouseRightButtonDown");

            if (this.isDrawing || this.isErasing)
                return;

            Ellipse ellipse = sender as Ellipse;
            string[] tempArr = ellipse.Name.Split('_');
            int group = int.Parse(tempArr[1]);

            DeleteTextAll();

            for (int i = group + 1; i < this.textGeometries.Count; i++)
            {
                this.textGeometries[i].Group--;
            }
            this.textGeometries.RemoveAt(group);

            DrawTextAll();
        }

        private void text_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            _log.Debug("text_MouseLeftButtonDown");

            if (this.isDrawing)
                return;

            Label label = sender as Label;
            string[] tempArr = label.Name.Split('_');
            int group = int.Parse(tempArr[1]);

            if (this.isErasing)
            {
                DeleteTextAll();

                for (int i = group + 1; i < this.textGeometries.Count; i++)
                {
                    this.textGeometries[i].Group--;
                }
                this.textGeometries.RemoveAt(group);

                DrawTextAll();

                return;
            }

            if (this.isDoubleClicked)
                return;

            this.isTextClicked = true;
            Point point = e.GetPosition(this.canvas);

            TextGeometry textGeometry = this.textGeometries[group];
            this.diffX = point.X - textGeometry.TextPoint.X;
            this.diffY = point.Y - textGeometry.TextPoint.Y;

            Mouse.Capture((FrameworkElement)sender);
        }

        private void text_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            _log.Debug("text_MouseLeftButtonUp");

            if (this.isTextClicked && !this.isDoubleClicked)
            {
                this.isTextClicked = false;

                Label label = sender as Label;
                string[] tempArr = label.Name.Split('_');
                int group = int.Parse(tempArr[1]);

                DeletePointer(group);
                DrawPointer(this.textGeometries[group].PointerPoint, group);

                DeleteLabel(constText, group);
                DrawText(this.textGeometries[group].TextPoint, group, this.textGeometries[group].Text);

                Mouse.Capture(null);
            }
        }

        private void text_MouseMove(object sender, MouseEventArgs e)
        {
            if (this.isTextClicked && !this.isDoubleClicked)
            {
                Label label = sender as Label;
                Point point = e.GetPosition(this.canvas);

                string[] tempArr = label.Name.Split('_');
                int group = int.Parse(tempArr[1]);

                TextGeometry textGeometry = this.textGeometries[group];

                if (point.X - this.diffX > this.canvas.ActualWidth - label.ActualWidth || point.X - this.diffX < 0)
                {
                    point.X = textGeometry.TextPoint.X;
                }
                else
                {
                    point.X = point.X - this.diffX;
                }

                if (point.Y - this.diffY > this.canvas.ActualHeight - label.ActualHeight || point.Y - this.diffY < 0)
                {
                    point.Y = textGeometry.TextPoint.Y;
                }
                else
                {
                    point.Y = point.Y - this.diffY;
                }

                Canvas.SetLeft(label, point.X);
                Canvas.SetTop(label, point.Y);

                Point diffPoint = new Point(point.X - this.diffX, point.Y - this.diffY);
                textGeometry.TextPoint = point;

                DeleteTextLine(this.textGeometries[group].Group);
                DrawTextLine(this.textGeometries[group].PointerPoint, this.textGeometries[group].TextPoint, this.textGeometries[group].Group);
            }
        }

        private void text_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            _log.Debug("text_MouseRightButtonDown");

            if (this.isDrawing || this.isErasing)
                return;

            Label label = sender as Label;
            string[] tempArr = label.Name.Split('_');
            int group = int.Parse(tempArr[1]);

            DeleteTextAll();

            for (int i = group + 1; i < this.textGeometries.Count; i++)
            {
                this.textGeometries[i].Group--;
            }
            this.textGeometries.RemoveAt(group);

            DrawTextAll();
        }

        private void textBox_LostFocus(object sender, RoutedEventArgs e)
        {
            _log.Debug("textBox_LostFocus");

            TextBox textBox = sender as TextBox;
            string[] tempArr = textBox.Name.Split('_');
            int group = int.Parse(tempArr[1]);

            this.textGeometries[group].Text = textBox.Text;

            this.canvas.MouseLeftButtonDown -= textbox_canvas_MouseLeftButtonDown;
            this.canvas.MouseLeave -= textbox_canvas_MouseLeave;
            this.canvas.Background = null;

            DeleteTextInput(group);
            DeleteTextLine(group);

            double labelWidth = GetLabelSize("StyleLabelText", textBox.Text).Width;

            if (this.canvas.ActualWidth - this.textGeometries[group].TextPoint.X < labelWidth)
            {
                this.textGeometries[group].TextPoint = new Point(this.canvas.ActualWidth - labelWidth, this.textGeometries[group].TextPoint.Y);
            }
            DrawTextLine(this.textGeometries[group].PointerPoint, this.textGeometries[group].TextPoint, this.textGeometries[group].Group);
            DrawText(this.textGeometries[group].TextPoint, group, textBox.Text);
        }

        private void textBox_KeyDown(object sender, KeyEventArgs e)
        {
            _log.Debug("textBox_KeyDown");

            if (e.Key == Key.Return)
            {
                TextBox textBox = sender as TextBox;

                FocusManager.SetFocusedElement(FocusManager.GetFocusScope((TextBox)sender), null);

                Keyboard.ClearFocus();
            }
        }

        private void textbox_canvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            _log.Debug("textbox_canvas_MouseLeftButtonDown");

            if (this.isDoubleClicked)
            {
                this.isDoubleClicked = false;
                return;
            }

            Canvas canvas = sender as Canvas;

            FocusManager.SetFocusedElement(FocusManager.GetFocusScope((Canvas)canvas), null);

            Keyboard.ClearFocus();
        }

        private void textbox_canvas_MouseLeave(object sender, MouseEventArgs e)
        {
            _log.Debug("textbox_canvas_MouseLeave");

            Canvas canvas = sender as Canvas;

            FocusManager.SetFocusedElement(FocusManager.GetFocusScope((Canvas)canvas), null);

            Keyboard.ClearFocus();
        }

        private void text_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            _log.Debug("text_MouseDoubleClick");

            this.isDoubleClicked = true;

            Label label = sender as Label;
            string[] tempArr = label.Name.Split('_');
            int group = int.Parse(tempArr[1]);

            DrawTextInput(this.textGeometries[group].TextPoint, group, this.textGeometries[group].Text);
        }

        //---------------------------------------------------------------------------------------------------- Function
        private void AddText(string isTextOn)
        {
            _log.Debug("AddText");

            if (CommandType != 0)
                DisableCommand();

            if (Convert.ToBoolean(isTextOn))
            {
                this.isDrawing = true;

                //Canvas 마우스 이벤트 활성화
                this.canvas.MouseLeftButtonDown += text_canvas_MouseLeftButtonDown;
                this.canvas.MouseRightButtonDown += text_canvas_MouseRightButtonDown;

                this.canvas.Background = Brushes.Transparent;
                CommandType = Constants.MeasureCmdText;
            }
        }

        private void DrawTextAll()
        {
            _log.Debug("DrawTextAll");

            foreach (var textGeometry in this.textGeometries)
            {
                DrawTextLine(textGeometry.PointerPoint, textGeometry.TextPoint, textGeometry.Group);
                DrawPointer(textGeometry.PointerPoint, textGeometry.Group);
                DrawText(textGeometry.TextPoint, textGeometry.Group, textGeometry.Text);
            }
        }

        private void DrawPointer(Point point, int group)
        {
            Ellipse ellipse = new Ellipse();
            ellipse.Name = constPointer + "_" + group;
            ellipse.Style = (Style)this.Resources["StyleEllipse"];
            ellipse.Stroke = Constants.AnnotationBrushes[group % Constants.AnnotationBrushes.Length];
            Canvas.SetLeft(ellipse, point.X);
            Canvas.SetTop(ellipse, point.Y);

            if (IsEditOn)
            {
                ellipse.MouseLeftButtonDown += pointer_MouseLeftButtonDown;
                ellipse.MouseLeftButtonUp += pointer_MouseLeftButtonUp;
                ellipse.MouseMove += pointer_MouseMove;
                ellipse.MouseRightButtonDown += pointer_MouseRightButtonDown;
            }

            this.canvas.Children.Add(ellipse);
        }

        private void DrawText(Point point, int group, string text)
        {
            DeleteLabel(constText, group);
            Label label = new Label();
            label.Style = (Style)this.Resources["StyleLabelText"];
            label.Name = constText + "_" + group;
            label.Content = text;
            label.BorderBrush = Constants.AnnotationBrushes[group % Constants.AnnotationBrushes.Length];
            Canvas.SetLeft(label, point.X);
            Canvas.SetTop(label, point.Y);

            if (IsEditOn)
            {
                label.MouseDoubleClick += text_MouseDoubleClick;
                label.MouseLeftButtonDown += text_MouseLeftButtonDown;
                label.MouseLeftButtonUp += text_MouseLeftButtonUp;
                label.MouseMove += text_MouseMove;
                label.MouseRightButtonDown += text_MouseRightButtonDown;
            }

            this.canvas.Children.Add(label);
        }

        private void DrawTextInput(Point point, int group, string text)
        {
            DeleteLabel(constText, group);
            TextBox textBox = new TextBox();
            textBox.Style = (Style)this.Resources["StyleTextBox"];
            textBox.Name = constTextBox + "_" + group;
            textBox.BorderBrush = Constants.AnnotationBrushes[group % Constants.AnnotationBrushes.Length];
            textBox.Text = text;
            Canvas.SetLeft(textBox, point.X);
            Canvas.SetTop(textBox, point.Y);

            this.canvas.Children.Add(textBox);

            //TextBox Focus Out을 위한 Event 처리
            textBox.LostFocus += textBox_LostFocus;
            textBox.KeyDown += textBox_KeyDown;
            this.canvas.MouseLeftButtonDown += textbox_canvas_MouseLeftButtonDown;
            this.canvas.MouseLeave += textbox_canvas_MouseLeave;
            this.canvas.Background = Brushes.Transparent;

            //TextBox Keyboard Focus 처리
            textBox.Focus();
            textBox.Select(textBox.Text.Length, 0);
            Keyboard.Focus(textBox);
        }

        private void DrawTextLine(Point pointerPoint, Point textPoint, int group)
        {
            Path path = new Path();
            path.Style = (Style)this.Resources["StylePath"];
            path.Data = CommonUtil.GetLine(pointerPoint, textPoint);
            path.Stroke = Constants.AnnotationBrushes[group % Constants.AnnotationBrushes.Length];
            path.Name = constTextLine + "_" + group;
            path.StrokeDashArray.Add(5);
            this.canvas.Children.Add(path);
        }

        private void DeleteTextAll()
        {
            _log.Debug("DeleteTextAll");

            foreach (var textGeometry in this.textGeometries)
            {
                DeleteTextLine(textGeometry.Group);
                DeletePointer(textGeometry.Group);
                DeleteLabel(constText, textGeometry.Group);
            }
        }

        private void DeletePointer(int group)
        {
            foreach (var item in this.canvas.Children)
            {
                if (item.GetType() == typeof(Ellipse))
                {
                    Ellipse temp = (Ellipse)item;

                    if (temp.Name == constPointer + "_" + group)
                    {
                        this.canvas.Children.Remove((Ellipse)item);
                        break;
                    }
                }
            }
        }

        private void DeleteTextLine(int group)
        {
            foreach (var item in this.canvas.Children)
            {
                if (item.GetType() == typeof(Path))
                {
                    Path temp = (Path)item;

                    if (temp.Name == constTextLine + "_" + group)
                    {
                        this.canvas.Children.Remove((Path)item);
                        break;
                    }
                }
            }
        }

        private void DeleteTextInput(int group)
        {
            foreach (var item in this.canvas.Children)
            {
                if (item.GetType() == typeof(TextBox))
                {
                    TextBox temp = (TextBox)item;

                    if (temp.Name == constTextBox + "_" + group)
                    {
                        this.canvas.Children.Remove((TextBox)item);
                        break;
                    }
                }
            }
        }


    }
}
