using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using System.Windows.Forms;

class RingApp : Form {
    delegate IntPtr HookProc(int code, IntPtr msg, IntPtr data);
    [StructLayout(LayoutKind.Sequential)] struct POINT { public int x, y; }
    [StructLayout(LayoutKind.Sequential)] struct MouseData { public POINT pt; public uint mouseData, flags, time; public IntPtr extra; }
    [DllImport("user32.dll", SetLastError=true)] static extern IntPtr SetWindowsHookEx(int id, HookProc proc, IntPtr module, uint thread);
    [DllImport("user32.dll")] static extern bool UnhookWindowsHookEx(IntPtr hook);
    [DllImport("user32.dll")] static extern IntPtr CallNextHookEx(IntPtr hook, int code, IntPtr msg, IntPtr data);
    [DllImport("kernel32.dll", CharSet=CharSet.Auto)] static extern IntPtr GetModuleHandle(string name);
    [DllImport("user32.dll")] static extern bool SetProcessDPIAware();
    [DllImport("user32.dll")] static extern IntPtr GetForegroundWindow();
    HookProc callback; IntPtr hook; ComboBox colors; TrackBar size; Label value; CheckBox enabled, follow; Panel preview; FollowHalo follower; Timer foregroundTimer; IntPtr lastForeground;
    static readonly Color[] palette = { Color.FromArgb(238,160,32), Color.FromArgb(32,152,215), Color.FromArgb(218,92,112), Color.FromArgb(32,163,125), Color.FromArgb(135,105,209), Color.FromArgb(222,115,54) };
    [STAThread] static void Main() { SetProcessDPIAware(); Application.EnableVisualStyles(); Application.SetCompatibleTextRenderingDefault(false); Application.Run(new RingApp()); }
    RingApp() {
        Text="ClickHalo"; ClientSize=new Size(370,340); FormBorderStyle=FormBorderStyle.FixedSingle; MaximizeBox=false; StartPosition=FormStartPosition.CenterScreen;
        var title=new Label { Text="ClickHalo 鼠标点击光圈", Font=new Font("Microsoft YaHei UI",14,FontStyle.Bold), AutoSize=true, Location=new Point(20,16) }; Controls.Add(title);
        enabled=new CheckBox { Text="点击显示", Checked=true, AutoSize=true, Location=new Point(24,58) }; Controls.Add(enabled);
        follow=new CheckBox { Text="跟随光标", AutoSize=true, Location=new Point(150,58) }; Controls.Add(follow);
        Controls.Add(new Label { Text="颜色", AutoSize=true, Location=new Point(24,96) });
        colors=new ComboBox { DropDownStyle=ComboBoxStyle.DropDownList, DrawMode=DrawMode.OwnerDrawFixed, ItemHeight=24, Location=new Point(85,91), Width=210 }; colors.Items.AddRange(new object[]{"琥珀金", "湖水蓝", "珊瑚粉", "翡翠绿", "鸢尾紫", "夕阳橙"}); colors.DrawItem += DrawColor; colors.SelectedIndex=0; Controls.Add(colors);
        Controls.Add(new Label { Text="大小", AutoSize=true, Location=new Point(24,139) });
        size=new TrackBar { Minimum=24, Maximum=64, TickFrequency=8, Value=40, Location=new Point(80,130), Width=210 }; value=new Label { Text="40 px", AutoSize=true, Location=new Point(294,139) }; size.ValueChanged += delegate { value.Text=size.Value+" px"; if(preview!=null) preview.Invalidate(); if(follower!=null) UpdateFollower(); }; Controls.Add(size); Controls.Add(value);
        Controls.Add(new Label { Text="实时预览", AutoSize=true, Location=new Point(24,190) });
        preview=new Panel { Location=new Point(85,183), Size=new Size(210,100), BackColor=Color.FromArgb(235,238,242) }; preview.Paint += PaintPreview; Controls.Add(preview);
        colors.SelectedIndexChanged += delegate { preview.Invalidate(); if(follower!=null) UpdateFollower(); };
        Controls.Add(new Label { Text="左键、右键和中键点击均可显示；关闭窗口即退出。", AutoSize=true, Location=new Point(24,305), ForeColor=Color.DimGray });
        callback=OnMouse; hook=SetWindowsHookEx(14,callback,GetModuleHandle(null),0); if(hook==IntPtr.Zero) { MessageBox.Show("无法启用鼠标监听，错误代码："+Marshal.GetLastWin32Error(),"鼠标光圈"); Shown += delegate { Close(); }; }
        foregroundTimer=new Timer { Interval=250 }; foregroundTimer.Tick += delegate { IntPtr foreground=GetForegroundWindow(); if(foreground!=lastForeground) { lastForeground=foreground; if(follower!=null) follower.RaiseOverlay(); } }; foregroundTimer.Start();
        enabled.CheckedChanged += delegate { if(enabled.Checked && follow.Checked) follow.Checked=false; };
        follow.CheckedChanged += delegate { if(follow.Checked) { if(enabled.Checked) enabled.Checked=false; UpdateFollower(); } else if(follower!=null) { follower.Close(); follower=null; } };
        FormClosed += delegate { foregroundTimer.Stop(); foregroundTimer.Dispose(); if(follower!=null) follower.Close(); if(hook!=IntPtr.Zero) UnhookWindowsHookEx(hook); };
    }
    void UpdateFollower() {
        if(follower==null || follower.IsDisposed) { follower=new FollowHalo(); follower.Show(); }
        follower.UpdateHalo(Cursor.Position, size.Value, palette[colors.SelectedIndex]);
    }
    void DrawColor(object sender, DrawItemEventArgs e) {
        if(e.Index<0) return;
        e.DrawBackground();
        using(var brush=new SolidBrush(palette[e.Index])) e.Graphics.FillEllipse(brush,e.Bounds.Left+8,e.Bounds.Top+5,14,14);
        TextRenderer.DrawText(e.Graphics,colors.Items[e.Index].ToString(),e.Font,new Rectangle(e.Bounds.Left+30,e.Bounds.Top,e.Bounds.Width-30,e.Bounds.Height),e.ForeColor,TextFormatFlags.VerticalCenter | TextFormatFlags.Left);
        e.DrawFocusRectangle();
    }
    void PaintPreview(object sender, PaintEventArgs e) {
        e.Graphics.SmoothingMode=SmoothingMode.AntiAlias;
        using(var dark=new SolidBrush(Color.FromArgb(45,51,62))) e.Graphics.FillRectangle(dark,preview.Width/2,0,preview.Width/2,preview.Height);
        float d=size.Value, y=(preview.Height-d)/2;
        using(var brush=new SolidBrush(Color.FromArgb(77,palette[colors.SelectedIndex]))) {
            e.Graphics.FillEllipse(brush,(preview.Width/4f)-d/2,y,d,d);
            e.Graphics.FillEllipse(brush,(preview.Width*3/4f)-d/2,y,d,d);
        }
    }
    IntPtr OnMouse(int code, IntPtr message, IntPtr data) {
        int m=message.ToInt32(); if(code>=0 && m==0x200 && follower!=null) { MouseData move=(MouseData)Marshal.PtrToStructure(data,typeof(MouseData)); follower.MoveTo(move.pt.x,move.pt.y); }
        if(code>=0 && enabled.Checked && (m==0x201 || m==0x204 || m==0x207)) { MouseData info=(MouseData)Marshal.PtrToStructure(data,typeof(MouseData)); var p=info.pt; int diameter=size.Value; Color c=palette[colors.SelectedIndex]; BeginInvoke((Action)delegate { new Ring(p.x,p.y,diameter,c).Show(); }); }
        return CallNextHookEx(hook,code,message,data);
    }
}
static class CaptureProtection {
    [DllImport("user32.dll", SetLastError=true)] public static extern bool SetWindowDisplayAffinity(IntPtr window, uint affinity);
    public static void Exclude(IntPtr window) { SetWindowDisplayAffinity(window, 0x00000011); }
}
class Ring : Form {
    const int WS_EX_TRANSPARENT=0x20, WS_EX_TOOLWINDOW=0x80, WS_EX_NOACTIVATE=0x08000000;
    readonly Timer timer=new Timer(); readonly DateTime start=DateTime.UtcNow; readonly int diameter; readonly Color color;
    protected override void OnHandleCreated(EventArgs e) { base.OnHandleCreated(e); CaptureProtection.Exclude(Handle); }
    protected override bool ShowWithoutActivation { get { return true; } }
    protected override CreateParams CreateParams { get { var p=base.CreateParams; p.ExStyle |= WS_EX_TRANSPARENT | WS_EX_TOOLWINDOW | WS_EX_NOACTIVATE; return p; } }
    public Ring(int x,int y,int d,Color c) { diameter=d; color=c; int extent=d+12; FormBorderStyle=FormBorderStyle.None; ShowInTaskbar=false; TopMost=true; StartPosition=FormStartPosition.Manual; Bounds=new Rectangle(x-extent/2,y-extent/2,extent,extent); BackColor=Color.Magenta; TransparencyKey=Color.Magenta; Opacity=0.30; DoubleBuffered=true; timer.Interval=25; timer.Tick += delegate { if((DateTime.UtcNow-start).TotalMilliseconds>=420) { timer.Stop(); Close(); } else { Opacity=0.30*(1-(DateTime.UtcNow-start).TotalMilliseconds/420); Invalidate(); } }; timer.Start(); }
    protected override void OnPaint(PaintEventArgs e) { base.OnPaint(e); e.Graphics.SmoothingMode=SmoothingMode.AntiAlias; double t=Math.Min(1,(DateTime.UtcNow-start).TotalMilliseconds/420); float d=(float)(diameter*(0.72+0.28*(1-(1-t)*(1-t)))); float x=(ClientSize.Width-d)/2, y=(ClientSize.Height-d)/2; using(var brush=new SolidBrush(color)) e.Graphics.FillEllipse(brush,x,y,d,d); }
    protected override void Dispose(bool disposing) { if(disposing) timer.Dispose(); base.Dispose(disposing); }
}


class FollowHalo : Form {
    [DllImport("user32.dll")] static extern bool SetWindowPos(IntPtr h, IntPtr after, int x, int y, int cx, int cy, uint flags);
    int diameter; Color color; int lastX=int.MinValue, lastY=int.MinValue;
    protected override void OnHandleCreated(EventArgs e) { base.OnHandleCreated(e); CaptureProtection.Exclude(Handle); }
    protected override bool ShowWithoutActivation { get { return true; } }
    protected override CreateParams CreateParams { get { var p=base.CreateParams; p.ExStyle |= 0x20 | 0x80 | 0x08000000; return p; } }
    public FollowHalo() { FormBorderStyle=FormBorderStyle.None; ShowInTaskbar=false; TopMost=true; StartPosition=FormStartPosition.Manual; BackColor=Color.Magenta; TransparencyKey=Color.Magenta; Opacity=0.30; DoubleBuffered=true; }
    public void UpdateHalo(Point point, int d, Color c) { int extent=d+8; bool changed=diameter!=d || color!=c; diameter=d; color=c; Bounds=new Rectangle(point.X-extent/2,point.Y-extent/2,extent,extent); lastX=point.X; lastY=point.Y; if(changed) Invalidate(); }
    public void MoveTo(int x, int y) { if(x==lastX && y==lastY) return; lastX=x; lastY=y; int offset=(diameter+8)/2; SetWindowPos(Handle,new IntPtr(-1),x-offset,y-offset,0,0,0x0001 | 0x0010); }
    public void RaiseOverlay() { if(IsHandleCreated && Visible) SetWindowPos(Handle,new IntPtr(-1),0,0,0,0,0x0001 | 0x0002 | 0x0010); }
    protected override void OnPaint(PaintEventArgs e) { base.OnPaint(e); e.Graphics.SmoothingMode=SmoothingMode.AntiAlias; float x=(ClientSize.Width-diameter)/2f, y=(ClientSize.Height-diameter)/2f; using(var brush=new SolidBrush(color)) e.Graphics.FillEllipse(brush,x,y,diameter,diameter); }
}


