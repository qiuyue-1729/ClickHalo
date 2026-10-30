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
    HookProc callback; IntPtr hook; ComboBox colors; TrackBar size, intensity; Label value; CheckBox enabled, follow; Panel preview; FollowHalo follower; Timer foregroundTimer; IntPtr lastForeground;
    static readonly Color[] palette = { Color.FromArgb(238,160,32), Color.FromArgb(32,152,215), Color.FromArgb(218,92,112), Color.FromArgb(32,163,125), Color.FromArgb(135,105,209), Color.FromArgb(222,115,54) };
    [STAThread] static void Main() { SetProcessDPIAware(); Application.EnableVisualStyles(); Application.SetCompatibleTextRenderingDefault(false); Application.Run(new RingApp()); }
    RingApp() {
        Text="ClickHalo"; ClientSize=new Size(370,390); FormBorderStyle=FormBorderStyle.FixedSingle; MaximizeBox=false; StartPosition=FormStartPosition.CenterScreen;
        var title=new Label { Text="ClickHalo 鼠标点击光圈", Font=new Font("Microsoft YaHei UI",14,FontStyle.Bold), AutoSize=true, Location=new Point(20,16) }; Controls.Add(title);
        enabled=new CheckBox { Text="点击显示", Checked=true, AutoSize=true, Location=new Point(24,58) }; Controls.Add(enabled);
        follow=new CheckBox { Text="跟随光标", AutoSize=true, Location=new Point(150,58) }; Controls.Add(follow);
        Controls.Add(new Label { Text="颜色", AutoSize=true, Location=new Point(24,96) });
        colors=new ComboBox { DropDownStyle=ComboBoxStyle.DropDownList, DrawMode=DrawMode.OwnerDrawFixed, ItemHeight=24, Location=new Point(85,91), Width=210 }; colors.Items.AddRange(new object[]{"琥珀金", "湖水蓝", "珊瑚粉", "翡翠绿", "鸢尾紫", "夕阳橙"}); colors.DrawItem += DrawColor; colors.SelectedIndex=0; Controls.Add(colors);
        Controls.Add(new Label { Text="大小", AutoSize=true, Location=new Point(24,139) });
        size=new TrackBar { Minimum=24, Maximum=64, TickFrequency=8, Value=40, Location=new Point(80,130), Width=210 }; value=new Label { Text="40 px", AutoSize=true, Location=new Point(294,139) }; size.ValueChanged += delegate { value.Text=size.Value+" px"; if(preview!=null) preview.Invalidate(); if(follower!=null) UpdateFollower(); }; Controls.Add(size); Controls.Add(value);
        Controls.Add(new Label { Text="浓淡", AutoSize=true, Location=new Point(24,188) });
        intensity=new TrackBar { Minimum=10, Maximum=80, TickFrequency=10, Value=35, Location=new Point(80,179), Width=210 };
        var intensityLabel=new Label { Text="35%", AutoSize=true, Location=new Point(294,188) }; Controls.Add(intensity); Controls.Add(intensityLabel);
        intensity.ValueChanged += delegate { intensityLabel.Text=intensity.Value+"%"; if(preview!=null) preview.Invalidate(); if(follower!=null) UpdateFollower(); };
        Controls.Add(new Label { Text="实时预览", AutoSize=true, Location=new Point(24,240) });
        preview=new Panel { Location=new Point(85,233), Size=new Size(210,100), BackColor=Color.FromArgb(235,238,242) }; preview.Paint += PaintPreview; Controls.Add(preview);
        colors.SelectedIndexChanged += delegate { preview.Invalidate(); if(follower!=null) UpdateFollower(); };
        Controls.Add(new Label { Text="左键、右键和中键点击均可显示；关闭窗口即退出。", AutoSize=true, Location=new Point(24,355), ForeColor=Color.DimGray });
        callback=OnMouse; hook=SetWindowsHookEx(14,callback,GetModuleHandle(null),0); if(hook==IntPtr.Zero) { MessageBox.Show("无法启用鼠标监听，错误代码："+Marshal.GetLastWin32Error(),"鼠标光圈"); Shown += delegate { Close(); }; }
        foregroundTimer=new Timer { Interval=250 }; foregroundTimer.Tick += delegate { IntPtr foreground=GetForegroundWindow(); if(foreground!=lastForeground) { lastForeground=foreground; if(follower!=null) follower.RaiseOverlay(); } }; foregroundTimer.Start();
        enabled.CheckedChanged += delegate { if(enabled.Checked && follow.Checked) follow.Checked=false; };
        follow.CheckedChanged += delegate { if(follow.Checked) { if(enabled.Checked) enabled.Checked=false; UpdateFollower(); } else if(follower!=null) { follower.Close(); follower=null; } };
        FormClosed += delegate { foregroundTimer.Stop(); foregroundTimer.Dispose(); if(follower!=null) follower.Close(); if(hook!=IntPtr.Zero) UnhookWindowsHookEx(hook); };
    }
    void UpdateFollower() {
        if(follower==null || follower.IsDisposed) { follower=new FollowHalo(); follower.Show(); }
        follower.UpdateHalo(Cursor.Position, size.Value, palette[colors.SelectedIndex], intensity.Value);
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
        int d=size.Value; using(var nebula=Nebula.Create(d,palette[colors.SelectedIndex],intensity.Value,1.0)) {
            e.Graphics.DrawImageUnscaled(nebula,(preview.Width/4)-d/2,(preview.Height-d)/2);
            e.Graphics.DrawImageUnscaled(nebula,(preview.Width*3/4)-d/2,(preview.Height-d)/2);
        }
    }
    IntPtr OnMouse(int code, IntPtr message, IntPtr data) {
        int m=message.ToInt32(); if(code>=0 && m==0x200 && follower!=null) { MouseData move=(MouseData)Marshal.PtrToStructure(data,typeof(MouseData)); follower.MoveTo(move.pt.x,move.pt.y); }
        if(code>=0 && enabled.Checked && (m==0x201 || m==0x204 || m==0x207)) { MouseData info=(MouseData)Marshal.PtrToStructure(data,typeof(MouseData)); var p=info.pt; int diameter=size.Value; int strength=intensity.Value; Color c=palette[colors.SelectedIndex]; BeginInvoke((Action)delegate { new Ring(p.x,p.y,diameter,c,strength).Show(); }); }
        return CallNextHookEx(hook,code,message,data);
    }
}
static class CaptureProtection {
    [DllImport("user32.dll", SetLastError=true)] public static extern bool SetWindowDisplayAffinity(IntPtr window, uint affinity);
    public static void Exclude(IntPtr window) { SetWindowDisplayAffinity(window, 0x00000011); }
}

static class Nebula {
    [StructLayout(LayoutKind.Sequential)] struct PointNative { public int X,Y; public PointNative(int x,int y) { X=x; Y=y; } }
    [StructLayout(LayoutKind.Sequential)] struct SizeNative { public int X,Y; public SizeNative(int x,int y) { X=x; Y=y; } }
    [StructLayout(LayoutKind.Sequential, Pack=1)] struct Blend { public byte Op,Flags,Alpha,Format; }
    [DllImport("user32.dll", SetLastError=true)] static extern bool UpdateLayeredWindow(IntPtr hwnd,IntPtr dst,ref PointNative pos,ref SizeNative size,IntPtr src,ref PointNative source,int key,ref Blend blend,int flags);
    [DllImport("user32.dll")] static extern IntPtr GetDC(IntPtr hwnd);
    [DllImport("user32.dll")] static extern int ReleaseDC(IntPtr hwnd,IntPtr dc);
    [DllImport("gdi32.dll")] static extern IntPtr CreateCompatibleDC(IntPtr dc);
    [DllImport("gdi32.dll")] static extern IntPtr SelectObject(IntPtr dc,IntPtr obj);
    [DllImport("gdi32.dll")] static extern bool DeleteObject(IntPtr obj);
    [DllImport("gdi32.dll")] static extern bool DeleteDC(IntPtr dc);
    public static Bitmap Create(int diameter,Color color,int intensity,double scale) {
        int n=Math.Max(1,diameter); var bitmap=new Bitmap(n,n,System.Drawing.Imaging.PixelFormat.Format32bppPArgb);
        double radius=n/2.0, maxAlpha=intensity*2.55*scale;
        for(int y=0;y<n;y++) for(int x=0;x<n;x++) {
            double dx=(x+0.5-radius)/radius, dy=(y+0.5-radius)/radius, r=Math.Sqrt(dx*dx+dy*dy);
            if(r>=1) continue;
            double cloud=0.80+0.20*Math.Cos(dx*3.0-dy*2.0)*Math.Cos(dy*3.5+dx*1.2);
            double fade=Math.Pow(1-r*r,1.5)*cloud;
            int alpha=Math.Max(0,Math.Min(255,(int)(maxAlpha*fade)));
            bitmap.SetPixel(x,y,Color.FromArgb(alpha,color.R,color.G,color.B));
        }
        return bitmap;
    }
    public static void Render(Form form,Bitmap bitmap,int x,int y) {
        IntPtr screen=GetDC(IntPtr.Zero), dc=CreateCompatibleDC(screen), hbitmap=IntPtr.Zero, old=IntPtr.Zero;
        try {
            hbitmap=bitmap.GetHbitmap(Color.FromArgb(0)); old=SelectObject(dc,hbitmap);
            var position=new PointNative(x,y); var size=new SizeNative(bitmap.Width,bitmap.Height); var source=new PointNative(0,0);
            var blend=new Blend { Op=0,Flags=0,Alpha=255,Format=1 };
            if(!UpdateLayeredWindow(form.Handle,screen,ref position,ref size,dc,ref source,0,ref blend,2)) throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error());
        } finally { if(old!=IntPtr.Zero) SelectObject(dc,old); if(hbitmap!=IntPtr.Zero) DeleteObject(hbitmap); if(dc!=IntPtr.Zero) DeleteDC(dc); if(screen!=IntPtr.Zero) ReleaseDC(IntPtr.Zero,screen); }
    }
}
class Ring : Form {
    readonly Timer timer=new Timer(); readonly DateTime start=DateTime.UtcNow; readonly int diameter,strength,centerX,centerY; readonly Color color;
    protected override void OnHandleCreated(EventArgs e) { base.OnHandleCreated(e); CaptureProtection.Exclude(Handle); }
    protected override bool ShowWithoutActivation { get { return true; } }
    protected override CreateParams CreateParams { get { var p=base.CreateParams; p.ExStyle |= 0x20 | 0x80 | 0x08000000 | 0x80000; return p; } }
    public Ring(int x,int y,int d,Color c,int alpha) { centerX=x;centerY=y;diameter=d;color=c;strength=alpha; FormBorderStyle=FormBorderStyle.None; ShowInTaskbar=false; TopMost=true; StartPosition=FormStartPosition.Manual; Bounds=new Rectangle(x-d/2,y-d/2,d,d); timer.Interval=30; timer.Tick += delegate { double t=(DateTime.UtcNow-start).TotalMilliseconds/420; if(t>=1) { timer.Stop(); Close(); } else Draw(t); }; }
    protected override void OnShown(EventArgs e) { base.OnShown(e); Draw(0); timer.Start(); }
    void Draw(double t) { int d=Math.Max(1,(int)(diameter*(0.72+0.28*(1-(1-t)*(1-t))))); using(var b=Nebula.Create(d,color,strength,1-t)) Nebula.Render(this,b,centerX-d/2,centerY-d/2); }
    protected override void Dispose(bool disposing) { if(disposing) timer.Dispose(); base.Dispose(disposing); }
}
class FollowHalo : Form {
    [DllImport("user32.dll")] static extern bool SetWindowPos(IntPtr h,IntPtr after,int x,int y,int cx,int cy,uint flags);
    int diameter,strength; Color color; int lastX=int.MinValue,lastY=int.MinValue;
    protected override void OnHandleCreated(EventArgs e) { base.OnHandleCreated(e); CaptureProtection.Exclude(Handle); }
    protected override bool ShowWithoutActivation { get { return true; } }
    protected override CreateParams CreateParams { get { var p=base.CreateParams; p.ExStyle |= 0x20 | 0x80 | 0x08000000 | 0x80000; return p; } }
    public FollowHalo() { FormBorderStyle=FormBorderStyle.None; ShowInTaskbar=false; TopMost=true; StartPosition=FormStartPosition.Manual; }
    public void UpdateHalo(Point point,int d,Color c,int alpha) { bool changed=diameter!=d || color!=c || strength!=alpha; diameter=d;color=c;strength=alpha;lastX=point.X;lastY=point.Y; if(changed) { using(var b=Nebula.Create(d,c,alpha,1.0)) Nebula.Render(this,b,point.X-d/2,point.Y-d/2); } else MoveTo(point.X,point.Y); }
    public void MoveTo(int x,int y) { if(x==lastX && y==lastY) return; lastX=x;lastY=y; SetWindowPos(Handle,new IntPtr(-1),x-diameter/2,y-diameter/2,0,0,0x0001 | 0x0010); }
    public void RaiseOverlay() { if(IsHandleCreated && Visible) SetWindowPos(Handle,new IntPtr(-1),0,0,0,0,0x0001 | 0x0002 | 0x0010); }
}


