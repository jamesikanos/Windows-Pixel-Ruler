using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace ScreenPixelRuler
{
    internal sealed class Ruler : Form
    {
        #region Win32 API Imports
        // ReSharper disable InconsistentNaming
        private const long LWA_ALPHA = 0x2L;
        private const int GWL_EXSTYLE = (-20);
        private const int WS_EX_LAYERED = 0x80000;
        public const int WM_NCLBUTTONDOWN = 0xA1;
        public const int HT_CAPTION = 0x2;

        [DllImport("user32.dll")]
        public static extern int SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);

        [DllImport("user32.dll")]
        public static extern bool ReleaseCapture();

        [DllImport("user32.dll")]
        private static extern bool SetLayeredWindowAttributes(IntPtr hwnd, uint crKey, byte bAlpha, uint dwFlags);

        [DllImport("user32.dll")]
        private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

        [DllImport("user32.dll")]
        private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

        [DllImport("User32.dll")]
        public static extern bool MoveWindow(IntPtr hWnd, int X, int Y, int nWidth, int nHeight, bool bRepaint);
        // ReSharper restore InconsistentNaming
        #endregion

        private const int BorderThickness = 5;
        private const int LineThickness = 3;

        private readonly ContextMenu _menu = new ContextMenu();
        private readonly MenuItem _exitItem = new MenuItem("Exit Ruler Application");

        public Ruler( int initialWidth )
        {
            Width = initialWidth;

            MinimumSize = new Size(300, 50);
            MaximumSize = new Size(SystemInformation.VirtualScreen.Width, 50);
            FormBorderStyle = FormBorderStyle.None;
            TopMost = true; 
            SizeChanged += delegate
            {
                //when the ruler is resized, invalidate the form so that it is repainted
                Invalidate();
            };

            _menu.MenuItems.Add(_exitItem);
            _exitItem.Click += delegate
            {
                //make the application quit when this item is clicked
                Application.Exit();
            };

            MouseUp += Ruler_MouseUp;
            MouseDown += Ruler_MouseDown;

            //make the ruler transparent using win32 api calls
            var opacity = (byte)((255 * 80) / 100);
            SetWindowLong(Handle, GWL_EXSTYLE, GetWindowLong(Handle, GWL_EXSTYLE) ^ WS_EX_LAYERED);
            SetLayeredWindowAttributes(Handle, 0, opacity, (uint)LWA_ALPHA);
        }

        private void Ruler_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left) return;

            //allows dragging of the ruler
            ReleaseCapture();
            //trick the application into thinking that the title bar is being clicked
            //this forces the application to move along with the mouse
            SendMessage(Handle, WM_NCLBUTTONDOWN, HT_CAPTION, 0);
        }

        protected override void WndProc(ref Message m)
        {
            const int wmNcHitTest = 0x84;
            const int htLeft = 10;
            const int htRight = 11;
            if (m.Msg == wmNcHitTest)
            {
                //get the x and y position of the mouse from the message
                var x = (int)(m.LParam.ToInt64() & 0xFFFF);
                var y = (int)((m.LParam.ToInt64() & 0xFFFF0000) >> 16);

                //change the x and y coordinates relative to the application
                var pt = PointToClient(new Point(x, y));
                
                //check if the mouse in an area which allows the mouse to resize, if so change to allow it
                if (pt.X <= ClientSize.Width && pt.X >= ClientSize.Width - BorderThickness )
                {
                    m.Result = (IntPtr) htRight;
                    return;
                }
                if (pt.X >= 0 && pt.X <= BorderThickness)
                {
                    m.Result = (IntPtr) htLeft;
                    return;
                }
            }
            base.WndProc(ref m);
        }

        private void Ruler_MouseUp(object sender, MouseEventArgs e)
        {
            //opens a context sensitive menu when the right button is pressed
            if (e.Button == MouseButtons.Right)
                _menu.Show(this, new Point(e.X, e.Y));
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            DrawRuler( e.Graphics);
        }

        private void DrawRuler(Graphics graphics)
        {
            var window = new Rectangle(0, 0, Width - 1, Height - 1);

            using (var borderPen = new Pen(Brushes.Black, BorderThickness))
            {
                using (var linePen = new Pen(Brushes.Black, LineThickness))
                {
                    graphics.FillRectangle(Brushes.DodgerBlue, window);
                    graphics.DrawRectangle(borderPen, window);

                    using (var font = new Font("Arial", 13))
                    {
                        for (var i = 0; i < Width; i += 50)
                        {
                            graphics.DrawLine(linePen, new Point(i, 0),
                                new Point(i, (2 - i % 100 / 50) * Height / 4));

                            if (i % 100 == 0)
                            {
                                graphics.DrawString(i + "px", font, Brushes.Black, new PointF(i, 27));
                            }
                        }
                    }
                }
            }
        }
    }
}
