using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Imaging;
using System.Drawing.Drawing2D;
using System.Text;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.Security;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Text.RegularExpressions;

//#define GRC

namespace fill {
    public enum MouseOp {None,Pan,Select
      ,Fill=10,FillShape,FillFlood,FillBorder,FillLinear,FillRadial,Fill4,FillVect,FillSquare,FillFloat,Replace
      ,Draw=40,DrawFree,DrawLine,DrawRect,DrawPolar,DrawEdge,DrawMorph
    }
    public enum RepeatOp { None,MirrorX,MirrorY,MirrorXY,Mirror8,Rotate,RotateM,Selection}
    public enum FilterOp { None,Invert,InvertIntensity,Saturate,Grayscale,Levels,Contrast,Border,Edge,ToWhite,ToBlack,Channel,Substract,Hue,Strips,Border2,Perm}
    public partial class fMain:Form,IMessageFilter {
        const int ZoomBase=120;
        internal FillPattern Pattern=new FillPattern(bmap.White);
        bmap map,undomap,redomap;
        string undocode,redocode;
        int[] undosel,redosel;
        Bitmap bm;
        ColorDialog CDialog=new ColorDialog();
        Brush BackBrush=Brushes.LightGray;
        bool Dirty=false;
        OpenFileDialog ofd=new OpenFileDialog();
        SaveFileDialog sfd=null;
        PrintDialog printd=null;
        PageSetupDialog paged=null;
        bool timeDraw=false,timeDirty=false;
        bool space,nospace,back,noback;
        int NoDraw=0;
        public bool MovePaste,MoveXor;
        int MoveTrColor;
        Bitmap MoveBits;
        int mseq=0; // mouse sequence
        int lmx,lmy; // last mouse position
        int pmx,pmy,pmcx,pmcy,pmcx2,pmcy2,pmk; // press mouse position and keys
        MouseButtons pmb;
        int[] mopt=null;
        int[] Morph=new int[16];
				PointPath gxy=new PointPath();
        int Morphs=0;
        public int Color1=0xff0000,Color2=0xffff00;
        internal int sx=0,sy=0,zoom=ZoomBase,angle=0,snap=0,freex=0,freey=0;
        double cos=1,sin=0;
        byte Height2=255;
        bool colorshift;
        bmap DrawBrush=null;        
        string DrawShape="-60,0 60,60 60,-60";// "-60,0 0,60 60,0 0,-60";
        int ShapeRotate,DelTotal;
        int[] Selection=new int[4] {1,0,0,0};
        bool movesel,mopundo;
        int bx,by,bx3,by3;
        MouseOp mop,LBop=MouseOp.FillShape,RBop=MouseOp.DrawLine,MBOp=MouseOp.Pan;
        int edge,edge2;
        string rtext="";
        int RepeatX,RepeatY,RepeatN; // 1 x-axis,2 y-axis,4 xy -axis
				bool RepeatCenter,RepeatOn;
        RepeatOp RepeatMode;
				fBoard Board;
				fHelp Help;
        IntPtr HKL=GDI.LoadKeyboardLayout("00000409",0);
        float[] DashOff=new float[1];
        float[] Dash=null;

        string[] arg;        
        
        int IX(int x,int y) { return (int)(((x-sx)*cos+(y-sy)*sin)*ZoomBase/zoom);}
        int IY(int x,int y) { return (int)((-(x-sx)*sin+(y-sy)*cos)*ZoomBase/zoom);}
        void S2I(int x,int y,out int ix,out int iy) {
          ix=IX(x,y);iy=IY(x,y);
        }
        int SX(int x,int y) { double rx=x*zoom/ZoomBase,ry=y*zoom/ZoomBase; return sx+(int)(rx*cos-ry*sin);}
        int SY(int x,int y) { double rx=x*zoom/ZoomBase,ry=y*zoom/ZoomBase; return sy+(int)(+rx*sin+ry*cos);}

        bool IsSnap(MouseOp op) {
          return op==MouseOp.DrawFree||op==MouseOp.DrawLine||op==MouseOp.DrawPolar||op==MouseOp.DrawRect||op==MouseOp.DrawEdge;
        }
        void Snap(ref int x,ref int y) {
          if(snap<2) return;
          x=snap*bmap.idiv(x+snap/2,snap);y=snap*bmap.idiv(y+snap/2,snap);
        }
        int SnapDist(int x,int y) {
          if(snap<2) return 0;
          int dx=x-snap*bmap.idiv(x+snap/2,snap),dy=y-snap*bmap.idiv(y+snap/2,snap);
          if(dx<0) dx=-dx;if(dy<0) dy=-dy;
          return dx>dy?dx:dy;
        }
        int AbsMaxDist(int dx,int dy) {
          if(dx<0) dx=-dx;if(dy<0) dy=-dy;
          return dx>dy?dx:dy;
        }
        int Dist(int dx,int dy) { return bmap.isqrt(dx*dx+dy*dy);}

        public fMain(string[] arg) {
            this.arg=arg;
            //for(int y=0;y<map.Data.Length;y++) map.Data[y]=(float)(y/512/512.0);
            //map.FuncRadial(Bez,Combine.Max,.5,.5,.3,1);           
            //map.FloodFill(0.9,0.9,0.33);

            InitializeComponent();
						Board=new fBoard(this);
            UpdateColors();
            UpdateMode();
        }
        public bool PreFilterMessage(ref Message m) {
          bool down;
          if((down=m.Msg==256)||m.Msg==257) { // key down&up
            Keys key=(Keys)m.WParam;            
            if(key==Keys.Space) {
              space=down;
              if(Board.TextInput) return false;
              if(!down) {
                if(nospace) nospace=false;
                else if(mop!=MouseOp.None) return ProcessCmdKey(ref m,key);
                else
                  BoardCmd();                
              } else nospace=false;
              return true;
            } else if(key==Keys.Back) {
              back=down;
              if(Board.TextInput) return false;
              return true;
            }
            return false;
          } else
            return false;  
        }
        protected override void OnLoad(EventArgs e) {        
          base.OnLoad(e);
          BackColor=Color.LightGray;
          string fname=arg.Length>0?arg[0]:null;
          if(fname!=null) {
            if(!LoadFile(fname,false,0,false)) {Close();return;}
            int a=1;
            bool comp=false;
            while(a<arg.Length) {
              string aa=arg[a++];
              if(aa=="-"||aa=="") break;
              if(!aa.StartsWith("-")) { a--;break;}
              string opt=aa.Length>2?aa.Substring(2):"";
              aa=aa.Substring(0,2);
              switch(aa) {
               case "-c":comp=true;break;
               case "-d":{int d;int.TryParse(opt,out d);Board.SetDiff(d);comp=true;break;}
              }
            }
            if(comp||a<arg.Length) {
              SetMouseOp(MouseButtons.Left,MouseOp.Select);
              if(!comp) Board.SetDiff(2);
              fname=a<arg.Length?arg[a]:":";
              if(fname==":"&&arg[0]==":") 
                if(DialogResult.Cancel==MessageBox.Show(this,"Next Clipboard","Bitmap compare",MessageBoxButtons.OKCancel,MessageBoxIcon.Question,MessageBoxDefaultButton.Button1)) fname="";
              if(fname+""!="") {
                Bitmap bm=LoadBitmap(fname,false,0);
                PasteSelection(bm,-1,Board.pasteTRX.Checked,Board.GetDiff(),false,false,false,Board.GetMix(),Board.GetPasteFilter(),1);
              }                  
            }
          }
          if(map==null) NewMap(false);
          UpdateBitmap();
          Repaint(true);
        }

        internal static float UpdateDash(float len,float off,int sqr) {
          return (float)(off+Math.Sqrt(sqr))%len;
        }
        internal void UpdateDash() {
          string s=Board.tbDash.Text.Trim();
          float[] dash=""+s==""||s[0]<'0'||s[0]>'9'?null:ParseFA(s);
          if(dash!=null&&(dash.Length&1)==1) {
            Array.Resize(ref dash,dash.Length+1);
            dash[dash.Length-1]=dash[dash.Length-2];
          }
          if(dash!=null) SumFA(dash);
          Dash=dash;
        }

        private void UpdateColors() {
          Board.bColor1.BackColor=bColor1.BackColor=IntColor(Color1);
          Board.bColor2.BackColor=bColor2.BackColor=IntColor(Color2);
          int s1=Palette.RGBSum(Color1);
          Board.bCLight1.BackColor=IntColor(Palette.ColorIntensity765(Color1,s1+48));
          Board.bCDark1.BackColor=IntColor(Palette.ColorIntensity765(Color1,s1-48));
          int s2=Palette.RGBSum(Color2);
          Board.bCLight2.BackColor=IntColor(Palette.ColorIntensity765(Color2,s2+48));
          Board.bCDark2.BackColor=IntColor(Palette.ColorIntensity765(Color2,s2-48));
        }

        public void UpdateBitmap() {
          if(bm!=null) bm.Dispose();
          if(map!=null) bm=new Bitmap(map.Width-2,map.Height-2,PixelFormat.Format32bppRgb);//PixelFormat.Format24bppRgb);          
        }        
        public void Repaint(bool dirty) { Repaint(dirty,true);}
        public void Repaint(bool dirty,bool xor) {
          if(NoDraw<1) {
            Repaint(0,0,map.Width-1,map.Height-1,dirty);
            if(xor) DrawXOR();
          }
        }
        int Clip(int x,int max) {
          return x<0?0:x>max?max:x;
        }
        
        public void Repaint(int x0,int y0,int x1,int y1,bmap brush) {
          if(brush!=null) {
            int d=brush.Width/2+1;
            x0-=d;x1+=d;
            d=brush.Height/2+1;
            y0-=d;y1+=d;
          }
          Repaint(x0,y0,x1,y1,true);
        }
        public void Repaint(int x0,int y0,int x1,int y1,bool dirty) {
				  x0--;y0--;x1++;y1++;
          int w=bm.Width-1,h=bm.Height-1;
          int r;
          if(x0>x1) {r=x0;x0=x1;x1=r;}
          if(y0>y1) {r=y0;y0=y1;y1=r;}
          x0=Clip(x0,w);x1=Clip(x1,w);
          y0=Clip(y0,h);y1=Clip(y1,h);
          if(dirty) bmap.ToBitmap(map,x0+1,y0+1,bm,x0,y0,x1,y1,false);
          Graphics gr=this.CreateGraphics();
          gr.InterpolationMode=zoom>ZoomBase&&angle<=0?InterpolationMode.NearestNeighbor:InterpolationMode.Default;
          bool full=x0<=0&&y0<=0&&x1>=w-1&&y1>=h-1;
          if(angle>0) {
            float z=zoom*1f/ZoomBase;
            gr.TranslateTransform(sx,sy);
            gr.RotateTransform(angle);
            gr.ScaleTransform(z,z);
            gr.DrawImage(bm,new Rectangle(x0,y0,x1-x0,y1-y0),x0,y0,x1-x0,y1-y0,GraphicsUnit.Pixel);
            if(full) {
              gr.FillRectangle(BackBrush,-2*Width,-2*Height,2*Width,4*Height+map.Height);
              gr.FillRectangle(BackBrush,map.Width,-2*Height,2*Width,4*Height+map.Height);
              gr.FillRectangle(BackBrush,0,-2*Height,map.Width,2*Height);
              gr.FillRectangle(BackBrush,0,map.Height,map.Width,2*Height);
            }
          } else {
            if(zoom>ZoomBase) {
              x0--;
              y0--;
            }
            int sx=SX(x0,y0),sy=SY(x0,y0),d2=zoom/ZoomBase/2;
            Rectangle rect=new Rectangle(sx,sy,zoom*(x1-x0+1)/ZoomBase,zoom*(y1-y0+1)/ZoomBase);
            //gr.DrawImage(bm,rect,x0,y0,x1-x0+1,y1-y0+1,GraphicsUnit.Pixel);
            gr.DrawImage(bm,rect,x0,y0,x1-x0+1,y1-y0+1,GraphicsUnit.Pixel);
            if(full) {
              int dy=zoom/ZoomBase/2;
              if(sy>0) gr.FillRectangle(BackBrush,0,0,Width,sy+d2);
              int ey=rect.Y+rect.Height;
              if(ey<Height) gr.FillRectangle(BackBrush,0,ey-d2,Width,Height-ey+d2);
              if(sx>0) gr.FillRectangle(BackBrush,0,sy,sx+d2,ey-sy);
              int ex=rect.X+rect.Width;
              if(ex<Width) gr.FillRectangle(BackBrush,ex-d2,sy,Width-ex+d2,ey-sy);
            }
          }
          gr.Dispose();
        }

        private void miExit_Click(object sender, EventArgs e) {
          Close();
        }

        void ZoomChange(int d,bool absolute,int key) {
          int zx,zy,z2=zoom;
          
          if(key>0) {zx=IX(lmx,lmy);zy=IY(lmx,lmy);} else zx=zy=0;          
          if(absolute) {
            if(d==int.MaxValue) {
              if(map!=null&&bm.Width>0&&bm.Height>0) {
                int zoom1=ZoomBase*Width/bm.Width;
                int zoom2=ZoomBase*Height/bm.Height;
                if(zoom1<zoom2) {
                  zoom=zoom1;
                  sx=0;sy=(Height-zoom*bm.Height/ZoomBase)/2;
                } else {
                  zoom=zoom2;
                  sx=(Width-zoom*bm.Width/ZoomBase)/2;sy=0;
                }
                return;
              } 
            } else {
              if(d<0) d=ZoomBase*-d;
              else if(d==0) d=ZoomBase;
              if(d<12) zoom=12;
              else if(d>ZoomBase*32) zoom=ZoomBase*32;
              else zoom=d;
            }
          } else if(d<0) {
            while(d<0&&zoom>=12) {
              zoom=zoom*3/4;
              d+=120;
            }
            if(zoom<12) zoom=12;
            else if(zoom>3*ZoomBase) zoom=bmap.round(zoom,ZoomBase);
          } else {
            while(d>0&&zoom<ZoomBase*32) {              
              zoom=zoom*4/3;
              d-=120;
            }
            if(zoom>ZoomBase*32) zoom=32*ZoomBase;
            else if(zoom>3*ZoomBase) zoom=bmap.round(zoom,ZoomBase);
          }
          if(key<1) { if(zoom>ZoomBase&&z2<ZoomBase||zoom<ZoomBase&&z2>ZoomBase) zoom=ZoomBase;}
          if(key==1) {
            sx+=lmx-SX(zx,zy);sy+=lmy-SY(zy,zx);
          } else if(key==2) {
            sx=Width/2-zx*zoom/ZoomBase;sy=Height/2-zy*zoom/ZoomBase;
            if(sx>0) sx=0;if(sy>0) sy=0;
            if(sx+bm.Width*zoom/ZoomBase<Width) sx=Width-bm.Width*zoom/ZoomBase;
            if(sy+bm.Height*zoom/ZoomBase<Height) sy=Height-bm.Height*zoom/ZoomBase;
          }  
        }
        void UpdateSin() {
          cos=Math.Cos(angle*Math.PI/180);sin=Math.Sin(angle*Math.PI/180);
        }
        void SetAngle(int x,int y,int a) {
          a%=360;
          if(a<0) a+=360;
          int ox=IX(x,y),oy=IY(x,y),nx;
          angle=a;
          UpdateSin();
          sx=0;sy=0;
          nx=x-SX(ox,oy);sy=y-SY(ox,oy);sx=nx;
        }
        protected override void OnMouseWheel(MouseEventArgs e) {
          int d=e.Delta;
          bool shift=GDI.ShiftKey,ctrl=GDI.CtrlKey;
          if(shift&&ctrl) {
            SetAngle(e.X,e.Y,angle+(-d/120*15));
            timeDraw=true;
            return;
          }
          if(shift|ctrl) {
            if(shift) sx+=d;
            else sy+=d;
            timeDraw=true;
            return;
          } 
          int x=IX(e.X,e.Y),y=IY(e.X,e.Y);          
          ZoomChange(d,false,0);
          sx=0;sy=0;
          sx=e.X-SX(x,y);sy=e.Y-SY(x,y);
          timeDraw=true;
        }
        public void Fullscreen() {
          bool f=FormBorderStyle!=FormBorderStyle.None;
          if(!f&&(sx!=0||sy!=0||zoom!=ZoomBase)) {
            sx=sy=0;zoom=ZoomBase;
            Repaint(false);
            return;
          }
          NoDraw++;
          //MainMenuStrip.Visible=!f;
          FormBorderStyle=f?FormBorderStyle.None:FormBorderStyle.Sizable;
          sx=sy=0;zoom=ZoomBase;
          NoDraw--;
          WindowState=f?FormWindowState.Maximized:FormWindowState.Normal;          
        }
        void Clear(bool white) {
          PushUndo();
					if(white) map.Clear(bmap.White);else map.LeaveBlack();
          sx=sy=0;zoom=ZoomBase;
          Repaint(true);
        }
        void Mirror(bool vertical,bool horizontal,bool adjust,bool copy) {
          if(IsShaping()) {
            DrawXOR();
            DrawMirror(horizontal,vertical,adjust);
            DrawXOR();
          } else if(IsSelecting()) {
            DrawXOR();
						int x=pmcx,y=pmcy,x2=IX(lmx,lmy),y2=IY(lmx,lmy);
						bool left=x2<x,top=y2<y;
						R.Norm(ref x, ref y, ref x2, ref y2);
						if(!mopundo) { PushUndo(); mopundo = true; }
						if(copy) {
						  int sx=x,sy=y,sx2=x2,sy2=y2;
							if(horizontal) {
								if(left) {
								  x=2*sx-sx2-1;x2=sx-1;
								  if(x<0) {sx2+=x;x=0;}
								} else {
								  x=sx2+1;x2=2*sx2-sx+1;
									if(x2>=bm.Width) {sx+=x2-bm.Width+1;x2=bm.Width-1;}
								}
								if(vertical) {
								  map.CopyRectangle(map,sx+1,sy+1,sx2+1,sy2+1,x+1,y+1,-1);
									map.Mirror(false,true,x+1,y+1,x2+1,y2+1);
									horizontal=false;
									Repaint(x,y,x2,y2,true);
									if(left) sx=x;else sx2=x2;
									x=sx;x2=sx2;
								}
						  }
							if(vertical) {
							  if(top) {
								  y=2*sy-sy2-1;y2=sy-1;
									if(y<0) {sy2+=y;y=0;}
								} else {
								  y=sy2+1;y2=2*sy2-sy+1;
									if(y2>=bm.Height) {sy+=y2-bm.Height+1;y2=bm.Height-1;}
								}
							}
						  map.CopyRectangle(map,sx+1,sy+1,sx2+1,sy2+1,x+1,y+1,-1);
						}
					  map.Mirror(vertical,horizontal,x+1,y+1,x2+1,y2+1);
            Repaint(x,y,x2,y2,true);
            DrawXOR();
          } else if(IsSelectionEmpty()) {
            map.Mirror(vertical,horizontal);Repaint(true);
          } else {
            int[] sel2=ClippedSelection();
            if(sel2==null) return;
            Undo4Move();
            DrawSelection();
            map.Mirror(vertical,horizontal,sel2[0]+1,sel2[1]+1,sel2[2]+1,sel2[3]+1);
            if(MoveBits!=null) bmap.Mirror(MoveBits,vertical,horizontal);
            Repaint(sel2[0],sel2[1],sel2[2],sel2[3],true);
            DrawSelection();
          }
        }
        void Rotate90(bool counter) {
          if(map==null) return;
          if(IsShaping()) {
            DrawXOR();
            ShapeRotate=(ShapeRotate+(counter?1:-1))&3;
            DrawXOR();
            return;
          } else if(IsSelecting()) {            
            int x=pmcx,y=pmcy,x2=IX(lmx,lmy),y2=IY(lmx,lmy);
            R.Norm(ref x,ref y,ref x2,ref y2);
            int[] rect=new int[] {0,0,bm.Width-1,bm.Height-1};
            if(!R.Intersect(rect,x,y,x2,y2)) return;
            x=rect[0];y=rect[1];
            x2=rect[2]-x;y2=rect[3]-rect[1];
            int size=x2<y2?x2:y2;
            DrawXOR();
            if(!mopundo) {PushUndo();mopundo=true;}
            bmap map2=map.Rotate90(counter,x+1,y+1,size,size);
            map.CopyRotate90(map2,x+1,y+1,false,false);
            Repaint(x,y,x+size,y+size,true);
            DrawXOR();          
            return;
          }          
          if(!IsSelectionEmpty()) {
            int[] sel2=MoveBits!=null?Selection.Clone() as int[]:ClippedSelection();
            if(sel2==null) return;
            int w=sel2[2]-sel2[0],h=sel2[3]-sel2[1];
            bool ex=IX(lmx,lmy)>(sel2[0]+sel2[2])/2,ey=IY(lmx,lmy)>(sel2[1]+sel2[3])/2;
            if(MoveBits!=null) {
              MoveBits=bmap.Rotate90(MoveBits,counter);
              map.CopyRectangle(undomap,Selection[0]+1,Selection[1]+1,Selection[2]+1,Selection[3]+1,Selection[0]+1,Selection[1]+1,-1);              
              map.CopyBitmap(MoveBits,Selection[0]+1,Selection[1]+1,MoveTrColor,Board.pasteTRX.Checked,Board.GetDiff(),Board.GetMix(),Board.GetPasteFilter());
            } else {
              bmap map2=map.Rotate90(counter,sel2[0]+1,sel2[1]+1,w+1,h+1);
              if(!Undo4Move()) 
                map.CopyRectangle(undomap,sel2[0]+1,sel2[1]+1,sel2[2]+1,sel2[3]+1,sel2[0]+1,sel2[1]+1,-1);
              map.CopyRotate90(map2,sel2[0]+1,sel2[1]+1,ex,ey);
            }            
            DrawSelection();
            int[] sel3=R.Copy(sel2);
            if(ex) {sel2[0]+=w-h;Selection[0]+=w-h;}
            if(ey) {sel2[1]+=h-w;Selection[1]+=h-w;}
            sel2[2]=sel2[0]+h;sel2[3]=sel2[1]+w;
            Selection[2]=Selection[0]+h;Selection[3]=Selection[1]+w;            
            R.Union(sel2,sel3);
            Repaint(sel2[0],sel2[1],sel2[2],sel2[3],true);
            DrawSelection();
            return;
          }
          map=map.Rotate90(counter);
          UpdateBitmap();
          
          int ix=IX(lmx,lmy),iy=IY(lmx,lmy),yx=ix,yy=iy;
          if(ix>=0&&iy>=0&&ix<map.Height-2&&iy<map.Width-2) {            
            if(counter) {yx=iy;yy=map.Height-2-ix;}
            else {yx=map.Width-2-iy;yy=ix;}
          } else {
            if(ix>map.Height/2-1) yx-=map.Height-map.Width;
            if(iy>map.Width/2-1) yy-=map.Width-map.Height;
          }
          
          int dx=yx-ix,dy=yy-iy;
          sx-=dx*zoom/ZoomBase;sy-=dy*zoom/ZoomBase;

          Repaint(true);
        }
				int[] Rect() {
					if(IsSelectionEmpty()) return new int[] {1,1,bm.Width,bm.Height};
					else return new int[] {Selection[0]+1,Selection[1]+1,Selection[2]+1,Selection[3]+1};
				}
        void RemoveDots(bool black,bool white,char mode) { PushUndo();map.RemoveDots(black,white,mode,undomap);Repaint(true);}
        void RemoveDust(int max,bool black,bool white) { 
          PushUndo();
          int[] r=Rect();
          if(white) map.RemoveDust(max,bmap.White,0,false,r[0],r[1],r[2],r[3]);
          if(black) map.RemoveDust(max,0,bmap.White,false,r[0],r[1],r[2],r[3]);
          Repaint(true);
        }
        void Knee(int mode,bool x,bool y,bool outer) { 
          PushUndo();
          int[] r=Rect();
          if(mode==8) map.Pixels(2+(x?2:0)+(y?4:0)+(outer?8:0),r[0],r[1],r[2],r[3]);
          else if(mode==7) map.Average(x,y,r[0],r[1],r[2],r[3]);
          else if(mode==6) map.Erase(r[0],r[1],r[2],r[3],x?4:3);
          else if(mode==5) map.Erase(r[0],r[1],r[2],r[3],x,y);
          else if(mode==4) map.Cone(r[0],r[1],r[2],r[3],y,Color2);
          else if(mode==3) map.Strip(r[0],r[1],r[2],r[3],x,y);
          else if(mode==2) map.Wedge(r[0],r[1],r[2],r[3],x,y);
          else if(mode==1) map.Corner(r[0],r[1],r[2],r[3],x,y);
          else map.Knee(r[0],r[1],r[2],r[3],x,y,outer);
          Repaint(true);
        }
        void PalN(int n,bool avg) { PushUndo();int[] r=Rect();map.PalN(n,avg,r[0],r[1],r[2],r[3]);Repaint(true);}
        void Satur2(bool desat,bool over,int alpha) { Filter1(bmap.FilterSatur,desat?'d':over?'o':'.',alpha);}
        void Saw(int mode,int alpha) { Filter1(bmap.Filter256,Palette.Saw256(mode),alpha);}
        void PalN(int n,bool avg,int alpha) { if(avg) PalN(n,avg); else Filter1(bmap.Filter256,Palette.n256(n),alpha);}
        void Bold(bool rgb,bool max,bool vert) { PushUndo();int[] r=Rect();map.Bold(rgb,max,vert,r[0],r[1],r[2],r[3]);Repaint(true); }
        void Gamma(bool inv,double gamma,int alpha) { Filter1(bmap.Filter256,Palette.Gamma(inv,gamma),alpha);}
        void Pal(string pal,int alpha) { Filter1(bmap.Filter765c,Palette.Render(Palette.Parse(pal.Split(',')),766),alpha);}
        void Pal1(string pal,int alpha) { Filter1(bmap.Filter765c,Palette.Render(Palette.Parse(pal),766),alpha);}        
        void Gammai(bool inv,double gamma,int alpha) { Filter1(bmap.Filter765i,Palette.Gammai(inv,gamma),alpha);}
        void ReplaceC(int scolor,int rcolor,int absolute) {Filter1(bmap.FilterReplace,new int[] { absolute>0?scolor:Palette.RGBSatur(scolor),rcolor,absolute},0); }
        void Bright(bool dark,bool bw,int level) { PushUndo();int[] r=Rect();map.Bright(dark,bw,level,r[0],r[1],r[2],r[3]);Repaint(true);}
        void NoWhite(bool white,bool black) { PushUndo();map.NoWhite(white,black); Repaint(true); }        
        void Contour(bool stroke,bool fill,bool inv) { PushUndo();int[] r=Rect();map.Contour(stroke,fill,inv,r[0],r[1],r[2],r[3]); Repaint(true); }
        void Comp(bool hori,bool vert,int lim) { PushUndo();int[] r=Rect();map.Comp(hori,vert,lim,r[0],r[1],r[2],r[3]);Repaint(true); }

        void RLine(bmap sqr,int y) {
          int s=sqr.Height;
          if(y>=s) y-=s;
          sqr.Line(0,y,y,0,0,false);
          if(y<s-1) sqr.Line(y+1,s-1,s-1,y+1,0,false);
        }
        void FLine(bmap sqr,int y) {
          int s=sqr.Height;
          if(y>=s) y-=s;
          sqr.Line(0,y,s-1-y,s-1,0,false);
          if(y>0) sqr.Line(s-y,0,s-1,y-1,0,false);
        }
        void Diamond(bmap sqr,int x,int y,int size) {          
          int w=sqr.Width,h=sqr.Height;
          int d=1-(size&1),e=1-d;
          size=(size-1)/2;
          for(int i=0;i<=size;i++)
            for(int j=0;j<=i;j++) {
              sqr.XY(bmap.modp(x+d+i-j,w),bmap.modp(y+d+j,h),0);
              sqr.XY(bmap.modp(x-i+j,w),bmap.modp(y-j,h),0);
              sqr.XY(bmap.modp(x-j,w),bmap.modp(y+d+i-j,h),0);
              sqr.XY(bmap.modp(x+d+j,w),bmap.modp(y-i+j,h),0);
            }                      
        }
        FillPattern GetPattern(string pattern,int size,int width) {
          FillPattern fp=new FillPattern(-1);
          fp.BMap=new bmap(size,size);
          fp.BMap.Clear(0xffffff);
          if(pattern==":"||pattern==".") {
            Diamond(fp.BMap,0,0,width);
            if(pattern==":")  Diamond(fp.BMap,(size+1)/2,(size+1)/2,width);
          } else if(pattern==",") {
            fp.BMap.FillRectangle(size-width,size-width,size-1,size-1,0);
          } else if(pattern=="+") {
            fp.BMap.FillRectangle(0,0,size-width-1,size-width-1,0);
            fp.BMap.FillRectangle(size-width,size-width,size-1,size-1,0);
          }
         else for(int j=0;j<width;j++) 
          switch(pattern) {
           case "/":
            RLine(fp.BMap,j);
            break;
           case "x":
           case "\\":
            FLine(fp.BMap,j);
            if(pattern=="x"&&size>1) {
              RLine(fp.BMap,j);
            }
            break;
           case "#":
           case "-":
            fp.BMap.Line(0,size-1-j,size-1,size-1-j,0,false);if(pattern=="#") goto case "|";
            break;
           case "|":
            fp.BMap.Line(size-1-j,0,size-1-j,size-1,0,false);
            break;
          }
          return fp;
        }
        void BWPattern(int color,string pattern,int size,int width,bool inv) {
          int[] r=Rect();
          FillPattern fp=GetPattern(pattern,size,width);
          if(inv) fp.BMap.Filter(FilterOp.Invert,0,true,0,0,size-1,size-1,null);
          PushUndo();
          map.Replace(color,0,r[0],r[1],r[2],r[3],fp);
          Repaint(true);
        }
        void FilterRect(int cmd) {
          PushUndo();
          int[] r=Rect();
          switch(cmd) {
           case 1:map.Color765bw2(r[0],r[1],r[2],r[3]);break;
           case 2:map.Color256x3(r[0],r[1],r[2],r[3],false);break;
           case 3:map.Color256x3(r[0],r[1],r[2],r[3],true);break;           
           default:
             if(cmd>512) map.Color765gro(r[0],r[1],r[2],r[3],cmd-512,true);
             else map.Color765grx(r[0],r[1],r[2],r[3],cmd-256,true);
             break;
          }          
          Repaint(true);
        }
        void Matrix(bool diffuse,bool rgb,bool abs,int level) {
          PushUndo();int[] r=Rect();
          if(diffuse) map.Diffuse(r[0],r[1],r[2],r[3],rgb,level);
          else map.Matrix(r[0],r[1],r[2],r[3],rgb,abs,level);
          Repaint(true);        
        }
        void DiffusePal(int n,int[] pal) {
          PushUndo();int[] r=Rect();
          map.Diffuse(r[0],r[1],r[2],r[3],n,pal);
          Repaint(true);        
        }
        void C4(int c00,int c01,int c10,int c11) {
          PushUndo();int[] r=Rect();
          map.C4(c00,c01,c10,c11,r[0],r[1],r[2],r[3]);
          Repaint(true);        
        }
        void Filter(FilterOp f,int param,bool bw) {
          PushUndo(f==FilterOp.Border?"Border":"");int[] r=Rect();map.Filter(f,param,bw,r[0],r[1],r[2],r[3],undomap);Repaint(true);
        }
        void Filter33(bmap.Filter33Delegate fx,object param,bool bw) {
          PushUndo();int[] r=Rect();map.Filter33(fx,param,bw,r[0],r[1],r[2],r[3],undomap);Repaint(true);
        }
        void Blur(int size,bool hori,bool vert,int sharp,int mul,int shape2) {
          PushUndo();int[] r=Rect();
            if(shape2>=0) map.Blur2(size,shape2,sharp,mul,r[0],r[1],r[2],r[3]);
            else map.Blur(size,hori,vert,sharp,mul,r[0],r[1],r[2],r[3]);
          Repaint(true);
        }
        void Shadow(int count,int color) {
          if(color==-2) color=map.XY(IX(lmx,lmy)+1,IY(lmx,lmy)+1);
          PushUndo();int[] r=Rect();map.Shadow(count,color,r[0],r[1],r[2],r[3]);Repaint(true);
        }
        void Fall(int color,int lim,bool d8,bool exp) {
          if(color==-2) color=map.XY(IX(lmx,lmy)+1,IY(lmx,lmy)+1);
          if(color==-1) color=bmap.White;
          PushUndo();int[] r=Rect();map.Fall(color,lim,d8,exp,r[0],r[1],r[2],r[3]);Repaint(true);
        }
        void Neq(int level,bool lt,bool gt,bool x8,int d8,int expand,int expand2,int color) {
          PushUndo();int[] r=Rect();map.Neq(level,lt,gt,x8,d8,expand,expand2,color,r[0],r[1],r[2],r[3]);Repaint(true);
        }
        void Outline(int x,int y,int count,bool x8,int color,int exp) {
          PushUndo();int[] r=Rect();if(map.Outline(x+1,y+1,count,GDI.CapsLock,x8,color,exp,r[0],r[1],r[2],r[3])) Repaint(true);
        }
        void Color256(int mode) {
          PushUndo();int[] r=Rect();map.Color256(mode,r[0],r[1],r[2],r[3]);Repaint(true);
        }
        void MaxCount(int count,bool max) {
          Cursor c2=Cursor;Cursor=Cursors.WaitCursor;
          PushUndo();int[] r=Rect();map.MaxCount(count,max,r[0],r[1],r[2],r[3]);Repaint(true);
          Cursor=c2;
        }
        void Color765(int mode) {
          PushUndo();int[] r=Rect();
           if(mode<1) map.Color765rgb(r[0],r[1],r[2],r[3]);
           else map.Color765(mode>2?2:mode>1?1:0,r[0],r[1],r[2],r[3]);
           Repaint(true);
        }
        void hdr4(int size,bool rgb,bool satur,bool diag,int n0) {
          Cursor c2=Cursor;Cursor=Cursors.WaitCursor;
          PushUndo();int[] r=Rect();map.hdr4(r[0],r[1],r[2],r[3],size,rgb,satur,diag,n0);Repaint(true);
          Cursor=c2;
        }
        void Filter1(bmap.Filter1Delegate f,object param,int alpha) {
          PushUndo();int[] r=Rect();
          if(0<map.Filter1(f,param,r[0],r[1],r[2],r[3],alpha)) Repaint(true);
        }
        void Replace(int search,int replace) {
          PushUndo();int[] r=Rect();map.Replace(search,replace,r[0],r[1],r[2],r[3]);Repaint(true);
        }
        void Replace(int search,bool x8,int incolor,int bcolor,int outcolor) {
          PushUndo();int[] r=Rect();map.Replace(search,x8,incolor,bcolor,outcolor,undomap,r[0],r[1],r[2],r[3]);Repaint(true);
        }
        void FillDiff(bool repl,int dx,int dy) {
          PushUndo();
          int[] r=Rect();
          int mode,diff;
          mode=Board.GetFillDiff(out diff);
          Cursor c2=Cursor;
          Cursor=Cursors.WaitCursor;
          if(dx>1||dy>1) map.FillDiff(1,1,bm.Width,bm.Height,dx,dy);
          if(repl) map.ReplDiff(r[0],r[1],r[2],r[3],mode,diff);
          else map.FillDiff(r[0],r[1],r[2],r[3],mode,diff,Board.cbFillDivCenter.Checked,true);
          Cursor=c2;
          Repaint(true);
        }

        int[] GetSel() {
          bool se=IsSelectionEmpty();
          return new int[4] {se?1:Selection[0],se?1:Selection[1],se?bm.Width:Selection[2],se?bm.Height:Selection[3]};
        }
        void Blur(bool closed) {
          PushUndo();
          int[] i4=GetSel();
          map.Filter(i4[0]+1,i4[1]+1,i4[2]+1,i4[3]+1,closed,0);
          Repaint(i4[0],i4[1],i4[2],i4[3],true);
        }
        void NoScale() {
          sx=sy=0;zoom=ZoomBase;
          Repaint(false);
        }
        void RGBShift(bool rgb2cmy,bool back,bool inv) {
          bool se=IsSelectionEmpty();
          if(rgb2cmy) {
            if(se) map.RGB2CMY(inv);else map.RGB2CMY(Selection[0]+1,Selection[1]+1,Selection[2]+1,Selection[3]+1,inv);
          } else { 
            if(!back) colorshift^=true;
            int mode=colorshift?1:3;
            if(se) map.RGBShift(mode);else map.RGBShift(mode,Selection[0]+1,Selection[1]+1,Selection[2]+1,Selection[3]+1);
            if(back) colorshift^=true;
          }
          Repaint(true);
        }
        void SetColor(int color12,int idx,bool shift) {
          SetStatusBar(false);
          int c=0;
          switch(idx) {
           case 1:c=shift?0xFF0088:0xff0000;break;
           case 2:c=shift?0xFF8800:0xffff00;break;
           case 3:c=shift?0x88FF00:0x00ff00;break;
           case 4:c=shift?0x00FF80:0x00ffff;break;
           case 5:c=shift?0x0088FF:0x0000ff;break;
           case 6:c=shift?0x8800FF:0xff00ff;break;
           case 7:c=shift?0x444444:0x000000;break;
           case 8:c=shift?0xcccccc:0xffffff;break;
           default:return;
          }
          if(color12==2) Color2=c;else Color1=c;
          UpdateColors();
        }
				internal void UpdateBoard() {
				  RepeatOn=Board.RepeatOn.Checked;
					RepeatX=X.t(Board.RepeatX.Text,RepeatX);
					RepeatY=X.t(Board.RepeatY.Text,RepeatY);			    
					RepeatCenter=Board.RepeatCenter.Checked;
				  RepeatN=X.t(Board.RepeatCount.Text,6);
					if(RepeatN<1) RepeatN=6;
					RepeatMode=Board.RepeatMode;
				}
				RepeatOp BoardRepeatMode() {
				  RepeatN=X.t(Board.RepeatCount.Text,6);
					if(RepeatN<1) RepeatN=6;
					RepeatCenter=Board.RepeatCenter.Checked;
					return Board.RepeatMode;
				}
				void Repeat(bool on,int x,int y) {
				  if(!on) {
					  Board.RepeatOn.Checked=RepeatOn=false;
						return;
					}
					Board.RepeatX.Text=""+(RepeatX=x);
					Board.RepeatY.Text=""+(RepeatY=y);
					Board.RepeatOn.Checked=RepeatOn=true;
				}
				void Erase(bool inner) {
				  int x=IX(lmx,lmy),y=IY(lmx,lmy),mode=0;
					if(inner) {
					  int dx=3*(x-Selection[0])/(Selection[2]-Selection[0]+1);
						int dy=3*(y-Selection[1])/(Selection[3]-Selection[1]+1);
						int d=dy*3+dx;
						mode=d==0||d==8?4:d==2||d==6?3:d==1||d==7?2:d==3||d==5?1:0;
					} else {
						int dx=x<Selection[0]?-1:x>Selection[2]?1:0;
						int dy=y<Selection[1]?-1:y>Selection[3]?1:0;
						if(dx==0) {
							if(dy==0) {
								int w=Selection[2]-Selection[0]+1,h=Selection[3]-Selection[1]+1,d=h<w?h:w;
								dx=x-Selection[0];dy=y-Selection[1];
								if(2*dx>w) dx=w-dx;if(2*dy>h) dy=h-dy;
								mode=2*(dx+dy)<d?5:0;
							} else mode=2;
						} else if(dy==0) mode=1;
						else mode=(dx<0)!=(dy<0)?3:4;
					}
					PushUndo();
					map.Erase(Selection[0]+1,Selection[1]+1,Selection[2]+1,Selection[3]+1,mode);
					Repaint(Selection[0],Selection[1],Selection[2],Selection[3],true);
					DrawXOR();

				}
				void Exchange(bool center) {
				  int x=IX(lmx,lmy),y=IY(lmx,lmy);
					if(x<0) x=0;else if(x>=bm.Width) x=bm.Width-1;
					if(y<0) y=0;else if(y>=bm.Height) y=bm.Height-1;
					if(R.Inside(Selection,x,y)) return;
					x++;y++;
					DrawXOR();
					R.Intersect(Selection,0,0,bm.Width-1,bm.Height-1);
			    int w=Selection[2]-Selection[0]+1,h=Selection[3]-Selection[1]+1;
					PushUndo();
					R.Shift(Selection,1,1);
					int dx=x<Selection[0]?-1:x>Selection[2]?1:0,dy=y<Selection[1]?-1:y>Selection[2]?1:0;
					bool doy;
					if(dx==0) {
					  if(dy==0) {
						  doy=Math.Min(y-Selection[1],Selection[3]-y)<Math.Min(x-Selection[0],Selection[2]-x);
						} else doy=true;
					} else {
					  if(dy==0) doy=false;
						else {
						  doy=Math.Max(y-Selection[3],Selection[1]-y)>Math.Max(x-Selection[2],Selection[0]-x);
						}
					}
					doy^=center;
					if(doy) {
					  if(center) y-=h/2;else if(y>Selection[3]-h/2) y-=h-1;
					  if(y<1) y=1;else if(y+h>=map.Height) y=map.Height-h-1;
						x=Selection[0];
					} else {
					  if(center) x-=w/2;else if(x>Selection[2]-w/2) x-=w-1;
					  if(x<1) x=1;else if(x+w>=map.Width) x=map.Width-w-1;
						y=Selection[1];
					}
					map.CopyRectangle(undomap,Selection[0],Selection[1],Selection[2],Selection[3],x,y,-1);
					if(x<Selection[0]) map.CopyRectangle(undomap,x,y,Selection[0]-1,Selection[3],Selection[2]-Selection[0]+x+1,y,-1);
					else if(x>Selection[0]) map.CopyRectangle(undomap,Selection[2]+1,y,Selection[2]+x-Selection[0],Selection[3],Selection[0],y,-1);
					if(y<Selection[1]) map.CopyRectangle(undomap,x,y,Selection[2],Selection[1]-1,x,Selection[3]-Selection[1]+y+1,-1);
					else if(y>Selection[1]) map.CopyRectangle(undomap,x,Selection[3]+1,Selection[2],Selection[3]+y-Selection[1],x,Selection[1],-1);
					Selection[0]=x;Selection[1]=y;Selection[2]=x+w-1;Selection[3]=y+h-1;
					R.Shift(Selection,-1,-1);
					Repaint(true);
				}
        void ChgMoveBits(bool repl,int x,int y) {
          bmap mb=bmap.FromBitmap(null,MoveBits,-1);
          x++;y++;
          bool c2=false;
          if(MoveTrColor==-1) MoveTrColor=Color2;
          else c2=mb.XY(x,y)==MoveTrColor;
          if(repl) {
            if(c2) {
              mb.Expand(GDI.CapsLock,false,null,new int[] {1,1,mb.Width-2,mb.Height-2},MoveTrColor,MoveTrColor,MoveTrColor,MoveTrColor);
            } else mb.Replace(mb.XY(x,y),MoveTrColor,0,0,mb.Width-1,mb.Height-1);
          } else {
            if(c2) {
              mb.FloodFill1(x,y,Board.X8.Checked,new int[] {1,1,mb.Width-2,mb.Height-2},-1,-1,MoveTrColor);
            } else {
              mb.FloodFill1(x,y,Board.X8.Checked,new int[] {1,1,mb.Width-2,mb.Height-2},MoveTrColor,MoveTrColor,-1);              
              //PointPath pp=new PointPath();
              //pp.Add(new PathPoint(x,y));
              //mb.FloodFill(new int[] {x,y},Color2,Color2,Board.X8.Checked,false,0,false,pp,false,null);
            }
          }
          bmap.ToBitmap(mb,MoveBits,false);
          map.CopyRectangle(undomap,Selection[0]+1,Selection[1]+1,Selection[2]+1,Selection[3]+1,Selection[0]+1,Selection[1]+1,-1);
          map.CopyBitmap(MoveBits,Selection[0]+1,Selection[1]+1,MoveTrColor,Board.pasteTRX.Checked,Board.GetDiff(),Board.GetMix(),Board.GetPasteFilter());
          DrawXOR();
          Repaint(Selection[0],Selection[1],Selection[2],Selection[3],true);
          DrawXOR();
        }
        protected override bool ProcessCmdKey(ref Message msg,Keys keyData) {
				  if(mop==MouseOp.None&&(keyData==(Keys.ControlKey|Keys.Control)||keyData==(Keys.Shift|Keys.ShiftKey)||keyData==(Keys.Alt|Keys.Menu))) return false;
          Keys k=keyData&~(Keys.Shift|Keys.Control|Keys.Alt);
          if(space&&k!=Keys.Space)
            nospace=true;
          if(back&&k!=Keys.Back)
            noback=true;
          if(k==Keys.Z||k==Keys.Y) {
            int scan=(msg.LParam.ToInt32()>>16)&127;
            k=(Keys)GDI.MapVirtualKeyEx(scan,3,HKL);
            keyData=k|(keyData&(Keys.Shift|Keys.Control|Keys.Alt));
            //k=scan<40?Keys.Y:Keys.Z;
          }
          switch(keyData) {
					  case Keys.F1:ProcessCommand("help");return true;
					  //case Keys.F10:BoardCmd();break;
            case Keys.F11:Fullscreen();return true;
            case Keys.Escape:              
              if(pmb!=MouseButtons.None)
                CancelMouse();
              else if(RepeatOn)
                Repeat(false,0,0);
              else if(!IsSelectionEmpty()) {
                CancelSelection();
              } else if(FormBorderStyle==FormBorderStyle.None) {
                Fullscreen();
              } else 
                NoScale();
              break;
          }
          bool ctrl=0!=(keyData&Keys.Control);
          bool shift=0!=(keyData&Keys.Shift);
          bool alt=0!=(keyData&Keys.Alt);
          if(alt) NoMovePaste();
          if(mop!=MouseOp.None) {
            int k3=(shift?1:0)|(ctrl?2:0)|(alt?4:0);
            switch(k) {
             case Keys.Tab:SwitchPress();break;
             case Keys.Delete:DrawXOR();DeleteInMop(mop,pmcx,pmcy,IX(lmx,lmy),IY(lmx,lmy),k3);DrawXOR();break;
             case Keys.Enter:
             case Keys.Space:FinishMop(mop,pmcx,pmcy,IX(lmx,lmy),IY(lmx,lmy));DrawXOR();break;            
             case Keys.Back:if(ctrl) {DrawXOR();pmcx=IX(lmx,lmy);pmcy=IY(lmx,lmy);DrawXOR();} else SwitchPress();break;
             case Keys.Left:MoveInMop(shift,ctrl,-1,0);break;
             case Keys.Right:MoveInMop(shift,ctrl,+1,0);break;
             case Keys.Up:MoveInMop(shift,ctrl,0,-1);break;
             case Keys.Down:MoveInMop(shift,ctrl,0,+1);break;
						 case Keys.R:
               if(mop==MouseOp.FillShape||mop==MouseOp.FillFloat) {
                 gxy.AddU(new PathPoint(IX(lmx,lmy)+1,IY(lmx,lmy)+1));
                 gxy.Set(GDI.CapsLock?"bf":"b");
                } break;
						 case Keys.E:case Keys.T:case Keys.D:case Keys.H:
               if(mop==MouseOp.FillShape||mop==MouseOp.FillFloat) {
                 gxy.AddU(new PathPoint(IX(lmx,lmy)+1,IY(lmx,lmy)+1));
                 gxy.Set((k==Keys.H?"h":k==Keys.D?"d":k==Keys.T?"t":"c")+(GDI.CapsLock?"f":""));
                }
                break;
						 case Keys.C:
               if(mop==MouseOp.DrawFree) {
                 MoveOp(pmcx2,pmcy2);
                 mop=MouseOp.None;
               } else if(mop==MouseOp.DrawLine) {
                 DrawXOR();
                 FinishMop(mop,pmcx,pmcy,pmcx2,pmcy2);
                 mop=MouseOp.None;
               } else if(mop==MouseOp.FillShape||mop==MouseOp.FillFloat) gxy.Closed=true;
               break;
             case Keys.V:if(mop==MouseOp.Select&&ctrl) { FinishMop(mop,pmcx,pmcy,IX(lmx,lmy),IY(lmx,lmy));ProcessCommand("paste");CancelMouse();};break;
						 case Keys.ShiftKey:
						  if(mop==MouseOp.FillShape||mop==MouseOp.FillLinear||mop==MouseOp.FillRadial||mop==MouseOp.FillFloat)
                gxy.AddU(new PathPoint(IX(lmx,lmy)+1,IY(lmx,lmy)+1,true));
							break;
						 case Keys.ControlKey:
						  if(mop==MouseOp.FillShape||mop==MouseOp.FillLinear||mop==MouseOp.FillRadial||mop==MouseOp.FillFloat) 
							  gxy.AddU(new PathPoint(IX(lmx,lmy)+1,IY(lmx,lmy)+1));
						  break;
             case Keys.O:
               DrawXOR();
               if(mop==MouseOp.DrawMorph) {
               if(Morphs==2) { Morph[4]=IX(lmx,lmy);Morph[5]=IY(lmx,lmy);Morphs++;}
               if(Morphs==3) { Morph[6]=Morph[4]+Morph[0]-Morph[2];Morph[7]=Morph[5]+Morph[1]-Morph[3];Morphs++;} 
               else if(Morphs==4) {
                 Morph[8]=IX(lmx,lmy);Morph[9]=IY(lmx,lmy);
                 int l=bmap.isqrt(Morph[2]-Morph[0],Morph[3]-Morph[1]);
                 Morph[10]=Morph[8]+l;Morph[11]=Morph[9]; 
                 l=bmap.isqrt(Morph[4]-Morph[2],Morph[5]-Morph[3]);
                 Morph[12]=Morph[10];Morph[13]=Morph[11]+l;
                 Morphs+=3;
               }
               if(Morphs==5) {
                 int l1=bmap.isqrt(Morph[2]-Morph[0],Morph[3]-Morph[1]),l2=bmap.isqrt(Morph[4]-Morph[2],Morph[5]-Morph[3]);
                 if(l1==0) l1=l2=1;
                 Morph[10]=IX(lmx,lmy);Morph[11]=IY(lmx,lmy);
                 //bool a=0<((Morph[10]-Morph[8])*(Morph[2]-Morph[0])+(Morph[11]-Morph[9])*(Morph[3]-Morph[1]));
                 int mul=-1;
                 Morph[12]=Morph[10]+mul*(Morph[11]-Morph[9])*l2/l1;Morph[13]=Morph[11]-mul*(Morph[10]-Morph[8])*l2/l1;
                 Morphs+=2;
               } else if(Morphs==6) { Morph[12]=IX(lmx,lmy);Morph[13]=IY(lmx,lmy);Morphs++;}
               if(Morphs==7) { Morph[14]=Morph[12]+Morph[8]-Morph[10];Morph[15]=Morph[13]+Morph[9]-Morph[11];
                 DrawMorph();
                 Repaint(true,false);
                 Morphs=4;
               } 
               } else Board.DrawOrto.Checked^=true;
               DrawXOR();
               break;
             default:goto nomk;
            }
            return true;
           nomk:;
          }
          switch(k) {
           case Keys.G:{
            int x=IX(lmx,lmy),y=IY(lmx,lmy); 
            if(MovePaste&&InSelection(x,y,0)) {
              if(MoveBits==null) {
                MoveBits=SelectionBitmap();
                MoveTrColor=Color2;
              }
              ChgMoveBits(true,x-Selection[0],y-Selection[1]);
              break;
            }
            int grd=(GDI.CapsLock?1:0);
            Replace("G",-1,ctrl?shift?0:Color2:shift?Color1:0,grd>0?GDI.NumLock?1:0:0,0,grd+(shift?1:0)+(ctrl?2:0));
            } break;
           case Keys.F7:
             if(!IsSelectionEmpty()) {
               if(ctrl) Paper(false,(int)Board.papW.Value,Board.chPaperB.Checked?0:Color1,Board.chPaperT.Checked?-1:bmap.White,InSelection(IX(lmx,lmy),IY(lmx,lmy),-10)?GDI.CtrlRKey?shift?8:4:shift?7:3:shift?9:11);             
               else Paper(false,(int)Board.papW.Value,Board.chPaperB.Checked?0:Color1,Board.chPaperT.Checked?-1:bmap.White,InSelection(IX(lmx,lmy),IY(lmx,lmy),-10)?alt?GDI.AltRKey?6:5:shift?GDI.ShiftRKey?2:1:0:alt?12:shift?10:13);
             };break;
           case Keys.F8:if(space) {int[] r=Rect();int n=map.ColorCount(r[0],r[1],r[2],r[3]);MessageBox.Show(this,""+n,"Color count "+(r[2]-r[0]+1)+"x"+(r[3]-r[1]+1)+" "+(r[2]-r[0]+1)*(r[3]-r[1]+1)); } else if(!IsSelectionEmpty()) DeleteRect(ctrl,shift);break;
           case Keys.F: {
            int x=IX(lmx,lmy),y=IY(lmx,lmy),c=map.XY(x,y);
            if(MovePaste&&InSelection(x,y,0)) {
              if(MoveBits==null) {
                MoveBits=SelectionBitmap();
                MoveTrColor=Color2;
              }
              ChgMoveBits(false,x-Selection[0],y-Selection[1]);
              break;
            }
            x++;y++;
            PushUndo("F");
            if(ctrl&&!shift) {
              map.FloodFill(new int[] {x,y},bmap.White,bmap.White,X8,false,0,GDI.CtrlRKey,gxy,false,null,Board.fillDown.Checked,Board.FillMix,Board.GetGammax());
              Repaint(true);
              break;
            }
            if(ctrl&&GDI.CtrlRKey&&shift) {
              int diff;
              map.FillDiff(x,y,Board.GetFillDiff(out diff),diff,Board.cbFillDivCenter.Checked,GDI.ShiftRKey?-1:-2);
              Repaint(true);               
              break;
            }
            int ctrlc=shift?Color2:bmap.White;
            if((ctrl?c==ctrlc&&!(!shift&&Fill2Black):c==Color1&&c==Color2&&!Fill2Black)) break;
            bool f2b=Fill2Black&&c!=0;
            if(ctrl) 
              map.FloodFill(new int[] {x,y},ctrlc,ctrlc,X8,f2b,0,!shift&&Fill2Black,gxy,false,null,Board.fillDown.Checked,Board.FillMix,Board.GetGammax());
            else {
  					  gxy.Add(new PathPoint(x,y));
              map.FloodFill(new int[] {x,y},Color1,Board.chFillMono.Checked?Color1:Color2,X8,FillNoBlack^shift,0,f2b,gxy,false,Pattern,Board.fillDown.Checked,Board.FillMix,Board.GetGammax());
						  gxy.Clear();
            }
            Repaint(true);
            } break;
           case Keys.I:ProcessCommand("invert"+(ctrl^Board.cbInvertBW.Checked?" bw":"")+(shift^Board.cbInvertIntensity.Checked?" intensity":""));break;              
           default:goto nok;
          }
          return true;
         nok:
          if(ctrl) {            
            switch(k) {
             case (Keys)192:if(shift) ExtendSelection(IX(lmx,lmy),IY(lmx,lmy),false,2);else BoardCmd(0);break;
             case Keys.D1:SetColor(1,1,shift);break;
             case Keys.D2:SetColor(1,2,shift);break;
             case Keys.D3:SetColor(1,3,shift);break;
             case Keys.D4:SetColor(1,4,shift);break;
             case Keys.D5:SetColor(1,5,shift);break;
             case Keys.D6:SetColor(1,6,shift);break;
             case Keys.D7:SetColor(1,7,shift);break;
             case Keys.D8:SetColor(1,8,shift);break;
             case Keys.O:OpenFile(shift,0);break;
             case Keys.C:ProcessCommand("copy");break;
             case Keys.V:ProcessCommand("paste");break;
             case Keys.R:if(shift) Extend(); else if(IsSelectionMode()) Shrink2();else Clear(GDI.CtrlRKey);break;
//             case Keys.O:hfmap.Load(map,"out.bb");Repaint();break;
//             case Keys.S:map.Save("out.bb");break;             
             case Keys.S:SaveFile(GDI.ShiftKey,false);break;
             case Keys.P:if(shift) PrintPage();else Print();break;
             case Keys.N:ProcessCommand(shift?"reload":"new");break;
             case Keys.E:NoScale();break;             
             case Keys.K:RGBShift(true,false,shift);break;
             case Keys.Z:if(shift) Redo();else Undo();break;
             case Keys.Y:if(shift) Undo();else Redo();break;
             case Keys.B:SetRelative(shift,ctrl,alt);break;
             case Keys.Left:if(!IsSelectionEmpty()) MoveSelection(false,GDI.CtrlRKey?-10:-1,0,!shift,false);else {KeyMove(-1,0,shift,alt,false);} break;
             case Keys.Right:if(!IsSelectionEmpty()) MoveSelection(false,GDI.CtrlRKey?+10:+1,0,!shift,false);else {KeyMove(+1,0,shift,alt,false);} break;
             case Keys.Up:if(!IsSelectionEmpty()) MoveSelection(false,0,GDI.CtrlRKey?-10:-1,!shift,false);else {KeyMove(0,-1,shift,alt,false);} break;
             case Keys.Down:if(!IsSelectionEmpty()) MoveSelection(false,0,GDI.CtrlRKey?+10:+1,!shift,false);else {KeyMove(0,+1,shift,alt,false);} break;             
             /*case Keys.Left:if(alt) MoveSelection(false,-1,0,shift,ctrl); else {sx+=shift?400:200;Repaint(false);} break;
             case Keys.Right:if(alt) MoveSelection(false,+1,0,shift,ctrl);else {sx-=shift?400:200;Repaint(false);} break;
             case Keys.Up:if(alt) MoveSelection(false,0,-1,shift,ctrl);else {sy+=shift?10:100;Repaint(false);} break;
             case Keys.Down:if(alt) MoveSelection(false,0,+1,shift,ctrl);else {sy-=shift?10:100;Repaint(false);} break;*/
             case Keys.PageUp:if(shift) ZoomChange(0,true,1);else sy+=Height/4;Repaint(false);break;
             case Keys.PageDown:if(shift) ZoomChange(int.MaxValue,true,1);else sy-=Height/4;Repaint(false);break;
             case Keys.Home:ZoomChange(shift?-8:-4,true,2);Repaint(false);break;
             case Keys.End:ZoomChange(-32,true,2);Repaint(false);break;
             case Keys.M:Mirror(true,true,false,GDI.CapsLock);break;
             case Keys.Insert:Insert(!shift,!ctrl);break;
             case Keys.Delete:Delete(true,shift);break;
             case (Keys)220:Expand(shift,true,1);Repaint(true);break;             
             case Keys.A:if(!Board.Visible) SelectAll();break;
             case Keys.W:BoardRepeatMode();if(shift) {RepeatMode=RepeatOp.Rotate;} else RepeatMode=RepeatOp.Mirror8;RepeatX=IX(lmx,lmy);RepeatY=IY(lmx,lmy);break;
             case Keys.F3:if(!IsSelectionEmpty()) FindRect(shift,false);break;
             case Keys.D:if(!IsSelectionEmpty()) Duplicate(shift,GDI.CtrlRKey);break;
						 case Keys.Back:if(!IsSelectionEmpty()) { if(!shift) ReplaceStrip(IX(lmx,lmy),IY(lmx,lmy),true,true);else Erase(false);} ;break;
             case Keys.NumPad7:Filter(FilterOp.Saturate,192,false);break;
             case Keys.NumPad8:Filter(FilterOp.Grayscale,192,false);break;
             case Keys.NumPad9:Filter(FilterOp.Levels,X.t(Board.levelsCount.Text,16),false);break;
             case Keys.NumPad6:Contour(true,GDI.CtrlRKey,false);break;
             case Keys.NumPad4:Expand(GDI.CtrlRKey,false,0);break;
             case Keys.NumPad5:Expand(GDI.CtrlRKey,true,1);break;
						 case Keys.T:DrawText(shift?GDI.ShiftRKey?1:-1:0,GDI.CtrlRKey?1:-1,IX(lmx,lmy),IY(lmx,lmy));break;
						 case Keys.Q:if(IsSelectionEmpty()) chColor.Checked^=true;else DrawShape2(GDI.ShiftKey,true,GDI.AltKey);break;
             default:goto ret;
            }
            return true;
          } else {
            switch(k) {
              case Keys.F5:Repaint(true);break;              
              case (Keys)192:if(alt) BoardCmd(0);else ExtendSelection(IX(lmx,lmy),IY(lmx,lmy),shift,0);break;
              case Keys.D1:if(alt) BoardCmd(1);else if(shift) SetMouseOp(GDI.ShiftRKey?MouseButtons.Left:MouseButtons.Right,MouseOp.DrawFree);else SetMouseOp(MouseButtons.Left,MouseOp.Select);break;
              case Keys.D2:if(alt) BoardCmd(2);else if(shift) SetMouseOp(GDI.ShiftRKey?MouseButtons.Left:MouseButtons.Right,MouseOp.DrawLine);else SetMouseOp(MouseButtons.Left,MouseOp.FillShape);break;
              case Keys.D3:if(alt) BoardCmd(3);else if(shift) SetMouseOp(GDI.ShiftRKey?MouseButtons.Left:MouseButtons.Right,MouseOp.DrawRect);else SetMouseOp(MouseButtons.Left,MouseOp.FillLinear);break;
              case Keys.D4:if(alt) BoardCmd(4);else if(shift) SetMouseOp(GDI.ShiftRKey?MouseButtons.Left:MouseButtons.Right,MouseOp.DrawPolar);else SetMouseOp(MouseButtons.Left,MouseOp.FillFlood);break;
              case Keys.D5:if(alt) BoardCmd(5);else if(shift) SetMouseOp(GDI.ShiftRKey?MouseButtons.Left:MouseButtons.Right,MouseOp.DrawEdge);else SetMouseOp(MouseButtons.Left,MouseOp.FillBorder);break;
              case Keys.D6:if(alt) BoardCmd(6);else if(shift) SetMouseOp(GDI.ShiftRKey?MouseButtons.Left:MouseButtons.Right,MouseOp.DrawMorph);else SetMouseOp(MouseButtons.Left,MouseOp.FillRadial);;break;
              case Keys.D7:if(alt) BoardCmd(7);else if(shift) SetMouseOp(GDI.ShiftRKey?MouseButtons.Left:MouseButtons.Right,MouseOp.DrawMorph);else {SetMouseOp(MouseButtons.Right,MouseOp.Replace);SetMouseOp(MouseButtons.Left,MouseOp.Select);}break;
              case Keys.D8:if(alt) BoardCmd(8);break;
              case Keys.D9:if(alt) BoardCmd(9);break;
              case (Keys)219:Color1=bmap.White&(map.XY(IX(lmx,lmy)+1,IY(lmx,lmy)+1));SetStatusBar(false);UpdateColors();break;
              case (Keys)221:Color2=bmap.White&(map.XY(IX(lmx,lmy)+1,IY(lmx,lmy)+1));SetStatusBar(false);UpdateColors();break;
              case Keys.Back:if(shift||IsSelectionEmpty()) {int r=Color1;Color1=Color2;Color2=r;;UpdateColors();} else ReplaceStrip(IX(lmx,lmy),IY(lmx,lmy),false,true);break;
              case Keys.M:Mirror(shift,!shift,false,GDI.CapsLock);break;
              case Keys.S:if(space) Shadow(4,-2);else if(shift) DarkColor(false,true,1);else SwapColors();break;
              case Keys.R:Rotate90(shift);break;
              case Keys.K:RGBShift(false,shift,false);break;
              case Keys.Z:if(!IsSelectionEmpty()) {ZoomTo(Selection[0],Selection[1],Selection[2],Selection[3],false,false);Repaint(false);} else ChooseColor(true);break;
              case Keys.X:ChooseColor(!shift);break;
              case (Keys)220:Expand((shift||GDI.CapsLock)^Board.X8.Checked,false,alt?GDI.AltRKey?2:3:0);Repaint(true);break;
              case Keys.B:if(alt) goto ret;else Blur(shift);break;
              case Keys.L:
               if(InSelection(IX(lmx,lmy),IY(lmx,lmy)))
                 Knee(shift?1:0,2*IX(lmx,lmy)>Selection[0]+Selection[2],2*IY(lmx,lmy)>Selection[1]+Selection[3],GDI.CapsLock);
               else {
                 int dx,dy;
                 bool hori=!OutSelection(IX(lmx,lmy),IY(lmx,lmy),out dx,out dy);
                 Knee(2,hori,(hori?dx:dy)>0,false);
                }
                 break;
              case Keys.Left:if(!IsSelectionEmpty()&&!MovePaste) MoveSelPoint(-1,0,shift);else KeyMove(-1,0,shift,alt,false);break;
              case Keys.Right:if(!IsSelectionEmpty()&&!MovePaste) MoveSelPoint(+1,0,shift);else KeyMove(+1,0,shift,alt,false);break;
              case Keys.Up:if(!IsSelectionEmpty()&&!MovePaste) MoveSelPoint(0,-1,shift);else KeyMove(0,-1,shift,alt,false);break;
              case Keys.Down:if(!IsSelectionEmpty()&&!MovePaste) MoveSelPoint(0,+1,shift);else KeyMove(0,+1,shift,alt,false);break;
              case Keys.PageUp:if(shift) sx+=Width/4;else ZoomChange(120,false,1);Repaint(false);break;
              case Keys.PageDown:if(shift) sx-=Width/4;else ZoomChange(-120,false,1);Repaint(false);break;
              case Keys.Home:ZoomChange(shift?-2:int.MaxValue,true,2);Repaint(false);break;
              case Keys.End:ZoomChange(shift?-32:-16,true,2);Repaint(false);break;
              case Keys.Subtract:ZoomChange(zoom>=3*ZoomBase?bmap.ceil(zoom,ZoomBase)-ZoomBase:zoom>ZoomBase*3/2?bmap.ceil(zoom,ZoomBase/2)-ZoomBase/2:zoom>ZoomBase?ZoomBase:zoom-ZoomBase/12,true,1);Repaint(false);break;
              case Keys.Add:ZoomChange(zoom<ZoomBase?zoom+ZoomBase/12:zoom<ZoomBase*3?bmap.floor(zoom,ZoomBase/2)+ZoomBase/2:bmap.floor(zoom,ZoomBase)+ZoomBase,true,1);Repaint(false);break;
              case Keys.Escape:if(!IsSelectionEmpty()) Delete(false,false);break;
              case Keys.Delete:
                if(alt) Remove(InSelection(IX(lmx,lmy),0,false),InSelection(0,IY(lmx,lmy),true));
                else Delete(false,shift);break;
              case Keys.Insert:Insert(!shift,!ctrl);break;
              case Keys.N:DrawXOR();if(shift) edge2++;else {edge2=++edge+1;};DrawXOR();break;
							case Keys.A:if(shift) DarkColor(false,false,1);else if(IsSelectionMode()) SmartSelect();break;
							case Keys.W:if(RepeatOn) {Repeat(false,0,0);break;} RepeatMode=InSelection()?RepeatOp.Selection:shift?RepeatOp.MirrorXY:BoardRepeatMode();Repeat(true,IX(lmx,lmy),IY(lmx,lmy));break;
              case Keys.F3:if(!IsSelectionEmpty()) FindRect(shift,true);break;
              case Keys.P:SetFillPattern(shift,false);break;
						  case Keys.E:if(IsSelectionMode()) Exchange(shift);break;
						  case Keys.T:DrawText(shift?GDI.ShiftRKey?1:-1:0,0,IX(lmx,lmy),IY(lmx,lmy));break;
              case Keys.Q:DrawShape2(GDI.ShiftKey,false,GDI.AltKey);break;              
              case (Keys)188:DarkColor(shift,false,1);break;
              case (Keys)190:DarkColor(shift,true,1);break;
              case Keys.NumPad5:ExtendSelection(IX(lmx,lmy),IY(lmx,lmy),false,2);break;
              case Keys.Clear:ExtendSelection(IX(lmx,lmy),IY(lmx,lmy),true,1);break;
              case Keys.O:{ int w=0+(shift?1:0)+(GDI.ShiftRKey?2:0);Outline(IX(lmx,lmy),IY(lmx,lmy),w,Board.X8.Checked,Color1,GDI.NumLock?0:w+1);} break;
              default: goto ret;
            }
            return true;
          } 
         ret:
          return base.ProcessCmdKey(ref msg,keyData);
        }
        void SwitchPress() {
          bool shift=GDI.ShiftKey;
          int x=pmcx,y=pmcy;
          pmcx=IX(lmx,lmy);pmcy=IY(lmx,lmy);
          if(shift) {
            int r=pmcy;pmcy=y;y=r;
          }
          lmx=SX(x,y);lmy=SY(x,y);
          int d;
          d=Width/8;if(lmx<0) {sx-=lmx-d;lmx=d;} else if(lmx>=Width) { sx-=lmx-Width+d;lmx=Width-d;}
          d=Height/8;if(lmy<0) {sy-=lmy-d;lmy=d;} else if(lmy>=Height) {sy-=lmy-Height+d;lmy=Height-d;}
          Point p=PointToScreen(new Point());          
          GDI.SetCursorPos(p.X+lmx,p.Y+lmy);
          Repaint(false);
        }
        public void SelectAll() {
          if(LBop!=MouseOp.Select) SetMouseOp(MouseButtons.Left,MouseOp.Select);
          DrawXOR();
          Selection[0]=Selection[1]=0;Selection[2]=bm.Width-1;Selection[3]=bm.Height-1;
          DrawXOR();
          if(IsStatusBar()) UpdateStatusBar();
        }
				void SmartSelect() {
				  DrawXOR();
					R.Intersect(Selection,0,0,bm.Width-1,bm.Height-1);
					R.Shift(Selection,1,1);
					map.SmartSelect(Selection);
					R.Shift(Selection,-1,-1);
				  DrawXOR();
				}
        void ExtendSelection(int x,int y,bool extend,int flood) {
          DrawXOR();
          bool e=IsSelectionEmpty();
          if(e) {
            Selection[0]=Selection[2]=x;Selection[1]=Selection[3]=y;
          } 
          if(flood>0||extend) {
            int[] rect=new int[] {1,1,map.Width-2,map.Height-2};
            fillres r=flood>0?map.FloodFill0(x+1,y+1,flood>1,rect):map.ColorExtent(x+1,y+1,rect);
            R.Union(Selection,new int[] {r.x0-1,r.y0-1,r.x1-1,r.y1-1});
          } else if(!e) {
            int b=0;
            if(x<Selection[0]) Selection[0]=x;else if(x>Selection[2]) {Selection[2]=x;b=1;}
            if(y<Selection[1]) Selection[1]=y;else if(y>Selection[3]) {Selection[3]=y;b=1;}
            if(b<1) {
              int d0=x-Selection[0],d1=y-Selection[1],d2=Selection[2]-x,d3=Selection[3]-y;
              if(d0<=d1&&d0<=d2&&d0<=d3) Selection[0]=x;
              else if(d1<=d0&&d1<=d2&&d1<=d3) Selection[1]=y;
              else if(d2<=d0&&d2<=d1&&d2<=d3) Selection[2]=x;
              else Selection[3]=y;
            }
          }
          DrawXOR();
        }
        void SetRelative(bool shift,bool ctrl,bool alt) {
          SetStatusBar(true);
          if(ctrl&&!shift) {
            string txt;
            if(GDI.CtrlRKey) {
              txt=tStatus.Text;              
              if(txt=="") txt=UpdateStatusBar();
            } else {
              int x,y,c,dx,dy;
              StatusInfo(out x,out y,out c,out dx,out dy);
              txt=x+"\t"+y+"\t"+dx+"\t"+dy+"\t"+(x-bx3)+"\t"+(y-by3)+"\t#"+c.ToString("X6")+"\t"+(c&255)+"\t"+((c>>8)&255)+"\t"+((c>>16)&255);
              bx3=x;by3=y;
            }
            rtext+=(rtext==""?"":"\r\n")+txt;
           //try { Clipboard.SetText(tStatus.Text);} catch {}
            return;
          }
          int bx2=bx,by2=by;
          bx=IX(lmx,lmy);by=IY(lmx,lmy);
          if(bx<0) bx=0;if(by<0) by=0;
          if(bx>=bm.Width) bx=bm.Width-1;
          if(by>=bm.Height) by=bm.Height-1;
          if(bx==bx2&&by==by2) bx=by=0;
          UpdateStatusBar();
        }
        void PutPixel(int color) {
          PushUndo();
          int ix=IX(lmx,lmy),iy=IY(lmx,lmy);
          map.XY(ix+1,iy+1,color);
          Repaint(ix,iy,ix,iy,true);
        }
        void Insert(bool vertical,bool horizontal) {
          if(map==null) return;
          PushUndo();
          if(IsSelectionMode()) {
					  if(IsSelectionEmpty()) {
						  int ix=IX(lmx,lmy)+1,iy=IY(lmx,lmy)+1;
						  map.Insert(vertical,horizontal,ix,iy,ix,iy);
						} else
              map.Insert(vertical,horizontal,Selection[0]+1,Selection[1]+1,Selection[2]+1,Selection[3]+1);
          } else 
            PutPixel(vertical?Color1:Color2);
          Repaint(true);
        }
        void Remove(bool vertical,bool horizontal) {
          PushUndo();
          map.Remove(vertical,horizontal,Selection[0]+1,Selection[1]+1,Selection[2]+1,Selection[3]+1,1,1,map.Width-2,map.Height-2);
          Repaint(true);
        }
        void Delete(bool ctrl,bool shift) {
          if(IsSelecting()) {
            if(!mopundo) {PushUndo();mopundo=true;}
            int c=shift?map.XY(IX(lmx,lmy)+1,IY(lmx,lmy)+1):-1;
            if(c<0) c=Color2;
            map.FillRectangle(pmcx+1,pmcy+1,IX(lmx,lmy)+1,IY(lmx,lmy)+1,c);
            //CancelMouse();
          } else if(IsSelectionMode()) {
            PushUndo();
						int ix=IX(lmx,lmy),iy=IY(lmx,lmy);
						bool inx=InSelection(ix,iy,false),iny=InSelection(ix,iy,true);
						if(inx&&iny)
						  map.FillRectangle(Selection[0]+1,Selection[1]+1,Selection[2]+1,Selection[3]+1,ctrl?shift?0:bmap.White:shift?Color1:Color2);
						else if(iny&&(ix==Selection[0]-1||ix==Selection[2]+1)||inx&&(iy==Selection[1]-1||iy==Selection[3]+1)) 
              map.FillRectangle(Selection[0]+1,Selection[1]+1,Selection[2]+1,Selection[3]+1,map.XY(ix+1,iy+1)&bmap.White);
            else
						  map.Erase(Selection[0]+1,Selection[1]+1,Selection[2]+1,Selection[3]+1,!inx,!iny);					
          } else {
            PutPixel(shift?0:0xffffff);
            return;
          }  
          Repaint(true);
        }
        int RepeatCount() {
          if(!RepeatOn) return 1;
          switch(RepeatMode) {
            case RepeatOp.MirrorX:
            case RepeatOp.MirrorY:return 2;
            case RepeatOp.MirrorXY:return 4;
            case RepeatOp.Mirror8:return 8;
            case RepeatOp.Rotate:return RepeatN;
            case RepeatOp.RotateM:return 2*RepeatN;
            case RepeatOp.Selection:return 9;
            default:return 1;
          }
        }
        int Repeat(int r,int x,int y,out int rx,out int ry) {
          rx=x;ry=y;int flags=0;
          if(r<1||!RepeatOn) return 0;
          switch(RepeatMode) {
           case RepeatOp.MirrorX:rx=2*RepeatX-rx;if(!RepeatCenter) {rx--;};flags=1;break;
           case RepeatOp.MirrorY:ry=2*RepeatY-ry;if(!RepeatCenter) {ry--;};flags=2;break;
           case RepeatOp.Mirror8:
           case RepeatOp.MirrorXY:
            bool mx=0!=(r&1),my=0!=(r&2);
            if(RepeatMode==RepeatOp.Mirror8&&0!=(r&4)) {
              int dx=rx-RepeatX,dy=ry-RepeatY;
              rx=RepeatX+dy;if(!RepeatCenter&&((dx>=0)!=(dy>=0))) rx+=0;
							ry=RepeatY+dx;
              flags=mx!=my?4:12;
            }            
            if(mx) {rx=2*RepeatX-rx;if(!RepeatCenter) {rx--;}}
            if(my) {ry=2*RepeatY-ry;if(!RepeatCenter) {ry--;}}
            flags^=(mx?1:0)|(my?2:0);
            break;
           case RepeatOp.Rotate:{					  
            double a=RepeatN<2?0:r%RepeatN*2*Math.PI/RepeatN;
            double si=Math.Sin(a),co=Math.Cos(a);
            x-=RepeatX;y-=RepeatY;
            rx=(int)Math.Round(co*x+si*y);ry=(int)Math.Round(-si*x+co*y);
            rx+=RepeatX;ry+=RepeatY;
            } break;
					 case RepeatOp.RotateM:{
					  if(x==RepeatX&&y==RepeatY) break;
					  bool m=0!=(r&1);r/=2;
						x-=RepeatX;y-=RepeatY;
						double b=Math.Atan2(y,x),c=Math.Sqrt(x*x+y*y);
						if(m) b=-b;
						b+=r*2*Math.PI/RepeatN;
            double si=Math.Sin(b),co=Math.Cos(b);						
						rx=(int)Math.Round(co*c);ry=(int)Math.Round(+si*c);
						rx+=RepeatX;ry+=RepeatY;
						} break;
           case RepeatOp.Selection: {
            int r2=r==0?4:r<5?r-1:r;
            int dx=(r2%3-1),dy=(r2/3)-1;
            int w=R.Width(Selection),h=R.Height(Selection);
            rx=x+dx*w;ry=y+dy*h;
            } break;
          }
          return flags;
        }
        void RepeatDrawLine(int x,int y,int x2,int y2,int color,bmap brush,bool repaint,bool xor,int arrow) {
          int rc=RepeatCount();
          if(rc>DashOff.Length) Array.Resize(ref DashOff,rc);
          for(int r=0;r<rc;r++) {
            int rx,ry,rx2,ry2;
            Repeat(r,x,y,out rx,out ry);
            Repeat(r,x2,y2,out rx2,out ry2);
            DrawLine(rx,ry,rx2,ry2,color,brush,repaint,xor,arrow,Dash,ref DashOff[r]);
          }
        }
        void FillPath(List<int> path,int color) {
          map.FillPath(path,1,1,color,Board.GetReplace('F'));
        }        
        void DrawLine(int x0,int y0,int x1,int y1,int color,bmap draw_brush,bool repaint,bool xor) {          
          float dashoff=0;
          DrawLine(x0,y0,x1,y1,color,draw_brush,repaint,xor,0,null,ref dashoff);
        }
        void DrawLine(int x0,int y0,int x1,int y1,int color,bmap draw_brush,bool repaint,bool xor,int arrow,float[] dash,ref float dashoff) {
          /*int r=Radius();
          x0=IX(x0);y0=IY(y0);x1=IX(x1);y1=IY(y1);
          map.FuncLine(x0,y0,x1,y1,r,Height2,draw,shape);            
          int s;
          if(x0>x1) {s=x0;x0=x1;x1=s;}
          if(y0>y1) {s=y0;y0=y1;y1=s;}
          Repaint(x0-r,y0-r,x1+r,y1+r);        
           */
          if(xor) {
            Graphics gr=CreateGraphics();
            IntPtr hdc=gr.GetHdc();
            int rop2=GDI.SetROP2(hdc,GDI.R2_NOTXORPEN);
            GDI.SelectObject(hdc,GDI.GetStockObject(GDI.WHITE_PEN));
            GDI.MoveToEx(hdc,SX(x0,y0),SY(x0,y0),IntPtr.Zero);
            GDI.LineTo(hdc,SX(x1,y1),SY(x1,y1));
            GDI.SetROP2(hdc,rop2);            
            gr.ReleaseHdc(hdc);
            gr.Dispose();
            return;            
          }
          bool point=x0==x1&&y0==y1;
          x0++;y0++;x1++;y1++;
          int ix=x0,iy=y0,zx=x1,zy=y1;          
          if(repaint) R.Norm(ref ix,ref iy,ref zx,ref zy);
          if(arrow!=0&&!point) {
            bool a0=0!=(arrow&1),a1=0!=(arrow&2),a2=0!=(arrow&4);
            int dx=x1-x0,dy=y1-y0,d=bmap.isqrt(bmap.sqr(dx,dy));
            int al,aw,ar;
            int.TryParse(Board.dArrowLen.Text,out al);int.TryParse(Board.dArrowWidth.Text,out aw);int.TryParse(Board.dArrrowRadius.Text,out ar);
            if(al<0) al=-al*d/100;if(aw<0) aw=-aw*d/100;
            int lx=dx*al,ly=dy*al,wx=dy*aw,wy=-dx*aw;
            int ax=(lx+wx/2)/d,ay=(ly+wy/2)/d,bx=(lx-wx/2)/d,by=(ly-wy/2)/d;
            int px=x1>x0?1:-1,py=y1>y0?1:-1,abx=Math.Abs(dx),aby=Math.Abs(dy);
            if(a0) {
              if(a2) {
                if(abx<=aby) {ax=bx=px*al;ay=aw/2;by=-aw/2;}
                else { ax=aw/2;bx=-aw/2;ay=by=py*al;}
              } 
              map.FillPath(new int[] {x0,y0,x0+ax,y0+ay,x0+bx,y0+by},Color2,0);
              map.BrushLine(x0,y0,x0+ax,y0+ay,color,draw_brush,BrushWhiteOnly);
              map.BrushLine(x0+ax,y0+ay,x0+bx,y0+by,color,draw_brush,BrushWhiteOnly);
              map.BrushLine(x0+bx,y0+by,x0,y0,color,draw_brush,BrushWhiteOnly);                            
              x0+=(ax+bx)/2;y0+=(ay+by)/2;
              if(repaint) {
                R.Union(ref ix,ref iy,ref zx,ref zy,x0+ax,y0+ay);
                R.Union(ref ix,ref iy,ref zx,ref zy,x0+bx,y0+by);
              }              
            }
            bool flat=Board.chRadiusFlat.Checked;
            if(a1) {
              if(a2) {
                if(abx>aby) {ax=bx=px*al;ay=aw/2;by=-aw/2;}
                else { ax=aw/2;bx=-aw/2;ay=by=py*al;}
              } 
              map.FillPath(new int[] {x1,y1,x1-ax,y1-ay,x1-bx,y1-by},Color2,0);
              map.BrushLine(x1,y1,x1-ax,y1-ay,color,draw_brush,BrushWhiteOnly);
              map.BrushLine(x1-ax,y1-ay,x1-bx,y1-by,color,draw_brush,BrushWhiteOnly);
              map.BrushLine(x1-bx,y1-by,x1,y1,color,draw_brush,BrushWhiteOnly);
              x1-=(ax+bx)/2;y1-=(ay+by)/2;
              if(repaint) {
                R.Union(ref ix,ref iy,ref zx,ref zy,x1-ax,y1-ay);
                R.Union(ref ix,ref iy,ref zx,ref zy,x1-bx,y1-by);
              }
            }
            if((a0||a1)&&d<=(a0&&a1?2*al:al)) goto repaint;
            if(a2) {
              if(abx>aby) {
                if(ar>aby) ar=aby;
                map.BrushLine(x0,y0,x0,y1-py*ar,color,draw_brush,BrushWhiteOnly);
                if(flat) 
                  map.BrushLine(x0,y1-py*ar,x0+px*ar,y1,color,draw_brush,BrushWhiteOnly);
                else
                  map.BrushQArc(x0,y1-py*ar,ar,px>0,py>0,true,color,draw_brush,BrushWhiteOnly);
                x0+=px*ar;y0=y1;
              } else {
                if(ar>abx) ar=abx;
                map.BrushLine(x0,y0,x1-px*ar,y0,color,draw_brush,BrushWhiteOnly);
                if(flat) 
                  map.BrushLine(x1-px*ar,y0,x1,y0+py*ar,color,draw_brush,BrushWhiteOnly);
                else
                  map.BrushQArc(x1-px*ar,y0,ar,px>0,py>0,false,color,draw_brush,BrushWhiteOnly);
                x0=x1;y0+=py*ar;
              }              
            }
          }
          map.BrushLine(x0,y0,x1,y1,color,draw_brush,BrushWhiteOnly,dash,dashoff);
          if(dash!=null) {
            dashoff=(float)((dashoff+Math.Sqrt(bmap.sqr(x0-x1,y0-y1))%dash[dash.Length-1]));
          }
         repaint:
          if(repaint) {
            Repaint(ix-1,iy-1,zx+1,zy+1,draw_brush);
          }
        }
        static Shape ParseShape(string shape) {
          if(string.IsNullOrEmpty(shape)) return null;
          string[] sa=shape.Split(',',' ');
          int n=sa.Length&~1;
          if(n<2) return null;
          Shape res=new Shape(n);
          for(int i=0;i<n;i++) {
            if(sa[i].StartsWith("m")) {
              if(res.move==null) res.move=new bool[n/2];
              res.move[i/2]=true;
              sa[i]=sa[i].Substring(1);
            }
            int.TryParse(sa[i],out res.pts[i]);
          }
          return res;
        }
				static Shape AdjustShape(Shape src,int width,int height,bool rotate) {
				  if(width==0||height==0) return src;
					int[] bb=Shape.BoundingBox(src.pts);
					if(bb==null) return src;
					if(rotate) { int r=width;width=height;height=r;}
				  if(width<0) width=-width;
					if(height<0) height=-height;				  
					int cx=(bb[2]+bb[0])/2,cy=(bb[1]+bb[3])/2,dx=bb[2]-bb[0],dy=bb[3]-bb[1];
					int delta=dx*height-dy*width;
					if(delta==0) return src;
					bool x=delta<0;
					if(x) delta=(width*dy/height-dx)/2;
					else delta=(height*dx/width-dy)/2;
					if(delta<=0) return src;
					Shape dst=new Shape(src);
					for(int i=0;i<dst.pts.Length;i+=2) {
					  if(x) {
						  if(dst.pts[i]<cx) dst.pts[i]-=delta;else if(dst.pts[i]>cx) dst.pts[i]+=delta;
						} else {
						  if(dst.pts[i+1]<cy) dst.pts[i+1]-=delta;else if(dst.pts[i+1]>cy) dst.pts[i+1]+=delta;
						}
					}
					return dst;				  
				}
        void XorLine(int x,int y,int x2,int y2) {
          Graphics gr=CreateGraphics();
          IntPtr hdc=gr.GetHdc();
          int rop2=GDI.SetROP2(hdc,GDI.R2_NOTXORPEN);
          GDI.SelectObject(hdc,GDI.GetStockObject(GDI.WHITE_PEN));
          GDI.MoveToEx(hdc,x,y,IntPtr.Zero);
          GDI.LineTo(hdc,x2,y2);
          GDI.SetROP2(hdc,rop2);
          gr.ReleaseHdc(hdc);
          gr.Dispose();
        }
        void XorCross(int x,int y,int x2,int y2) {
          Graphics gr=CreateGraphics();
          IntPtr hdc=gr.GetHdc();
          int rop2=GDI.SetROP2(hdc,GDI.R2_NOTXORPEN);
          GDI.SelectObject(hdc,GDI.GetStockObject(GDI.WHITE_PEN));
          GDI.MoveToEx(hdc,SX(x,y),SY(x,y),IntPtr.Zero);
          GDI.LineTo(hdc,SX(x2,y2),SY(x2,y2));          
          /*if(x!=x2) {
            GDI.MoveToEx(hdc,x,0,IntPtr.Zero);
            GDI.LineTo(hdc,x,Math.Min(y,y2));
            GDI.MoveToEx(hdc,x,Math.Max(y,y2),IntPtr.Zero);
            GDI.LineTo(hdc,x,Height);
          }
          if(y!=y2) {
            GDI.MoveToEx(hdc,0,y,IntPtr.Zero);
            GDI.LineTo(hdc,Math.Min(x,x2),y);
            GDI.MoveToEx(hdc,Math.Max(x,x2),y,IntPtr.Zero);
            GDI.LineTo(hdc,Width,y);
          }*/
          GDI.SetROP2(hdc,rop2);
          gr.ReleaseHdc(hdc);
          gr.Dispose();
        }
        void Swap2(ref int x,ref int y,ref int x2,ref int y2) {
          int r;r=x;x=x2;x2=r;r=y;y=y2;y2=r;
        }
        void RepeatDrawRect(int x,int y,int x2,int y2,int color,bmap draw_brush,string shape,int rotate,bool mirrorx,bool mirrory,bool adjust,bool xor,int fill) {
          //Snap(ref x,ref y);Snap(ref x2,ref y2);
          for(int r=0;r<RepeatCount();r++) {
            int rx,ry,rx2,ry2,rf;
            rf=Repeat(r,x,y,out rx,out ry);
            Repeat(r,x2,y2,out rx2,out ry2);
            //if(0!=(rf&1)) mirrorx^=true;
            //if(0!=(rf&2)) mirrory^=true;            
            int rrotate=rotate+(rf>>2)&3;
            //if(0!=(rf&2)) Swap2(ref rx,ref ry,ref rx2,ref ry2);
            DrawRect(rx,ry,rx2,ry2,color,draw_brush,shape,rrotate,mirrorx,mirrory,adjust,xor,fill);
          }
        }
        void DrawRect(int x0,int y0,int x1,int y1,int color,bmap draw_brush,string shape,int rotate,bool mirrorx,bool mirrory,bool adjust,bool xor,int fill) {
          Shape sh=ParseShape(shape==null&&!(xor&&!adjust&&rotate==0)?ShapeByName("rectangle"):shape);
					if(adjust) sh=AdjustShape(sh,x1-x0,y1-y0,rotate==1||rotate==3);
          int r;
          if(x0>x1) {r=x0;x0=x1;x1=r;mirrorx^=true;}
          if(y0>y1) {r=y0;y0=y1;y1=r;mirrory^=true;}
          Graphics gr=null;
          IntPtr hdc=IntPtr.Zero;
          int rop2=0;
          if(xor) {
            gr=CreateGraphics();
            hdc=gr.GetHdc();
            rop2=GDI.SetROP2(hdc,GDI.R2_NOTXORPEN);
            GDI.SelectObject(hdc,GDI.GetStockObject(GDI.WHITE_PEN));
          }
          List<int> path=xor?null:new List<int>();
          if(sh==null) {
            if(xor) {
              int sx0=SX(x0,y0),sy0=SY(x0,y0),sx1=SX(x1,y1),sy1=SY(x1,y1);
              if(zoom>=ZoomBase) {
                int d=zoom/ZoomBase/2;
                sx0-=d;sy0-=d;sx1+=d;sy1+=d;
              }
              if(sx0==sx1&&sy0==sy1) {
                GDI.MoveToEx(hdc,sx0-8,sy0,IntPtr.Zero);
                GDI.LineTo(hdc,sx0+9,sy0);
                GDI.MoveToEx(hdc,sx0,sy0-8,IntPtr.Zero);
                GDI.LineTo(hdc,sx0,sy0+9);
              } else {
                GDI.MoveToEx(hdc,sx0,sy0,IntPtr.Zero);
                if(sx1!=sx0) GDI.LineTo(hdc,sx1,sy0);
                if(sy1!=sy0) {
                  GDI.LineTo(hdc,sx1,sy1);
                  if(sx1!=sx0) {
                    GDI.LineTo(hdc,sx0,sy1);
                    GDI.LineTo(hdc,sx0,sy0);
                  }
                }
              }
            } else {
              path.Add(x0);path.Add(y0);
              path.Add(x1);path.Add(y0);
              path.Add(x1);path.Add(y1);
              path.Add(x0);path.Add(y1);
            }
          } else {
            int sx=x0,sy=y0,dx=x1-sx,dy=y1-sy,tx=0,ty=0,tmx=mirrorx?-1:1,tmy=mirrory?-1:1;
            switch(rotate&3) {
             case 0:tx=1;ty=0;break;
             case 1:tx=0;ty=-1;break;
             case 2:tx=-1;ty=0;break;
             case 3:tx=0;ty=1;break;
            }
            int[] pts=new int[sh.pts.Length];
            for(int i=0;i<sh.pts.Length;i+=2) {
              int vx=tmx*sh.pts[i],vy=tmy*sh.pts[i+1];
              int rx=vx*tx-vy*ty,ry=vx*ty+vy*tx;
              pts[i]=rx;pts[i+1]=ry;
            }
            int[] bb=Shape.BoundingBox(pts);            
            bb[2]-=bb[0];bb[3]-=bb[1];
            Shape.Move(pts,-bb[0],-bb[1]);
            int x=sx+pts[pts.Length-2]*dx/bb[2],y=sy+pts[pts.Length-1]*dy/bb[3];
            x0=x1=x;y0=y1=y;            
            for(int i=0;i<pts.Length;i+=2) {
              int nx=sx+pts[i]*dx/bb[2],ny=sy+pts[i+1]*dy/bb[3];
              bool m=sh.move!=null&&sh.move[i/2];
              if(!m) {
                if(xor) {
                  GDI.MoveToEx(hdc,SX(x,y),SY(x,y),IntPtr.Zero);
                  GDI.LineTo(hdc,SX(nx,ny),SY(nx,ny));                  
                } else {path.Add(nx);path.Add(ny);}//DrawLine(x,y,nx,ny,color,draw_brush,false,xor);
              }
              x=nx;y=ny;
              if(x<x0) x0=x;else if(x>x1) x1=x;
              if(y<y0) y0=y;else if(y>y1) y1=y;
            }
          }
          if(xor) {
            GDI.SetROP2(hdc,rop2);
            gr.ReleaseHdc(hdc);
            gr.Dispose();
          } else {
            if(fill>=0) FillPath(path,fill);
            if(color>=0&&(Board.DrawStroke.Checked||fill<0)) {
              float dashoff=0;
              for(int i=2;i<path.Count;i+=2) 
                DrawLine(path[i-2],path[i-1],path[i],path[i+1],color,draw_brush,false,xor,0,Dash,ref dashoff);
              DrawLine(path[path.Count-2],path[path.Count-1],path[0],path[1],color,draw_brush,false,xor,0,Dash,ref dashoff);
            }
            Repaint(x0-1,y0-1,x1+1,y1+1,draw_brush);
          }
        }
        void RepeatDrawPara(int x0,int y0,int x1,int y1,int x2,int y2,int color,bmap draw_brush,string shape,int rotate,bool mirrorx,bool mirrory,bool adjust,bool xor,int fill) {
          for(int r=0;r<RepeatCount();r++) {
            int rx0,ry0,rx1,ry1,rx2,ry2;
            Repeat(r,x0,y0,out rx0,out ry0);
            Repeat(r,x1,y1,out rx1,out ry1);
            Repeat(r,x2,y2,out rx2,out ry2);
            DrawPara(rx0,ry0,rx1,ry1,rx2,ry2,color,draw_brush,shape,rotate,mirrorx,mirrory,adjust,xor,fill);
          }
        }

        void DrawPara(int x0,int y0,int x1,int y1,int x2,int y2,int color,bmap draw_brush,string shape,int rotate,bool mirrorx,bool mirrory,bool adjust,bool xor,int fill) {
          Shape sh=ParseShape(shape);
					if(adjust) {
					  int width=(int)Math.Sqrt((x1-x0)*(x1-x0)+(y1-y0)*(y1-y0));
						int height=(int)Math.Sqrt((x2-x1)*(x2-x1)+(y2-y1)*(y2-y1));
						sh=AdjustShape(sh,width,height,rotate==1||rotate==3);
					}
          int r;
          //if(x0>x1) {r=x0;x0=x1;x1=r;mirrorx^=true;}
          //if(y0>y1) {r=y0;y0=y1;y1=r;mirrory^=true;}
          Graphics gr=null;
          IntPtr hdc=IntPtr.Zero;
          int rop2=0;
          if(xor) {
            gr=CreateGraphics();
            hdc=gr.GetHdc();
            rop2=GDI.SetROP2(hdc,GDI.R2_NOTXORPEN);
            GDI.SelectObject(hdc,GDI.GetStockObject(GDI.WHITE_PEN));
          }
          List<int> path=xor?null:new List<int>();            
          if(sh==null) {
            if(xor) {
              int sx0=SX(x0,y0),sy0=SY(x0,y0),sx1=SX(x1,y1),sy1=SY(x1,y1),sx2=SX(x2,y2),sy2=SY(x2,y2);
              if(zoom>=ZoomBase) {
                int d=zoom/ZoomBase/2;
                sx0-=d;sy0-=d;sx1+=d;sy1+=d;sx2-=d;sy2-=d;
              }
              GDI.MoveToEx(hdc,sx0,sy0,IntPtr.Zero);
              GDI.LineTo(hdc,sx1,sy1);
              GDI.LineTo(hdc,sx2,sy2);
              GDI.LineTo(hdc,sx2+sx0-sx1,sy2+sy0-sy1);
              GDI.LineTo(hdc,sx0,sy0);
            } else {
              path.Add(x0);path.Add(y0);
              path.Add(x1);path.Add(y1);
              path.Add(x2);path.Add(y2);
              path.Add(x2+x0-x1);path.Add(y2+y0-y1);
            }
          } else {
            int sx=x0,sy=y0,dx=x1-sx,dy=y1-sy,tx=0,ty=0,dx2=x2-x1,dy2=y2-y1,tmx=mirrorx?-1:1,tmy=mirrory?-1:1;
            switch(rotate&3) {
             case 0:tx=1;ty=0;break;
             case 1:tx=0;ty=-1;break;
             case 2:tx=-1;ty=0;break;
             case 3:tx=0;ty=1;break;
            }
            int[] pts=new int[sh.pts.Length];
            for(int i=0;i<sh.pts.Length;i+=2) {
              int vx=tmx*sh.pts[i],vy=tmy*sh.pts[i+1];
              int rx=vx*tx-vy*ty,ry=vx*ty+vy*tx;
              pts[i]=rx;pts[i+1]=ry;
            }
            int[] bb=Shape.BoundingBox(pts);
            bb[2]-=bb[0];bb[3]-=bb[1];
            Shape.Move(pts,-bb[0],-bb[1]);
            int x=sx+pts[pts.Length-2]*dx/bb[2]+pts[pts.Length-1]*dx2/bb[3];
            int y=sy+pts[pts.Length-2]*dy/bb[2]+pts[pts.Length-1]*dy2/bb[3];
            x0=x1=x;y0=y1=y;            
            for(int i=0;i<pts.Length;i+=2) {
              int nx=sx+pts[i]*dx/bb[2]+pts[i+1]*dx2/bb[3],ny=sy+pts[i]*dy/bb[2]+pts[i+1]*dy2/bb[3];
              bool m=m=sh.move!=null&&sh.move[i/2];
              if(!m) {
                if(xor) {
                  GDI.MoveToEx(hdc,SX(x,y),SY(x,y),IntPtr.Zero);
                  GDI.LineTo(hdc,SX(nx,ny),SY(nx,ny));                  
                } else {path.Add(nx);path.Add(ny);}//DrawLine(x,y,nx,ny,color,draw_brush,false,xor);                  
              }
              x=nx;y=ny;
              if(x<x0) x0=x;else if(x>x1) x1=x;
              if(y<y0) y0=y;else if(y>y1) y1=y;
            }
          }
          if(xor) {
            GDI.SetROP2(hdc,rop2);
            gr.ReleaseHdc(hdc);
            gr.Dispose();
          } else {
            if(fill>=0) FillPath(path,fill);
            if(color>=0&&(Board.DrawStroke.Checked||fill<0)) {
              float dashoff=0;
              for(int i=2;i<path.Count;i+=2) 
                DrawLine(path[i-2],path[i-1],path[i],path[i+1],color,draw_brush,false,xor,0,Dash,ref dashoff);
              DrawLine(path[path.Count-2],path[path.Count-1],path[0],path[1],color,draw_brush,false,xor,0,Dash,ref dashoff);          
            }
            Repaint(x0,y0,x1,y1,draw_brush);
          }
        }
        void RepeatDrawPolar(int x0,int y0,int x1,int y1,int color,bmap draw_brush,string shape,int rotate,bool mirrorx,bool mirrory,bool xor,int fill) {
          for(int r=0;r<RepeatCount();r++) {
            int rx0,ry0,rx1,ry1,rf;
            rf=Repeat(r,x0,y0,out rx0,out ry0);
            Repeat(r,x1,y1,out rx1,out ry1);            
            //if(0!=(rf&1)) Swap2(ref rx0,ref ry0,ref rx1,ref ry1);
            DrawPolar(rx0,ry0,rx1,ry1,color,draw_brush,shape,rotate,mirrorx,mirrory,xor,fill);
          }
        }
        void DrawPolar(int x0,int y0,int x1,int y1,int color,bmap draw_brush,string shape,int rotate,bool mirrorx,bool mirrory,bool xor,int fill) {
          int r,sx=x0,sy=y0,dx=x1-sx,dy=y1-sy,tx=0,ty=0,tmx=mirrorx?-1:1,tmy=mirrory?-1:1;
          if(dx==0&&dy==0) return;
          Shape sh=ParseShape(shape);
          if(sh==null) return;
          switch(rotate&3) {
           case 0:tx=1;ty=0;break;
           case 1:tx=0;ty=-1;break;
           case 2:tx=-1;ty=0;break;
           case 3:tx=0;ty=1;break;
          }
          Graphics gr=null;
          IntPtr hdc=IntPtr.Zero;
          int rop2=0;
          if(xor) {
            gr=CreateGraphics();
            hdc=gr.GetHdc();
            rop2=GDI.SetROP2(hdc,GDI.R2_NOTXORPEN);
            GDI.SelectObject(hdc,GDI.GetStockObject(GDI.WHITE_PEN));
          }
					int[] bb=Shape.BoundingBox(sh.pts);
          bb[2]-=bb[0];bb[3]-=bb[1];
					int ShapeBase=bb[2],ShapeHalf=bb[2]/2;
          List<int> path=xor?null:new List<int>();          
          int vx=tmx*sh.pts[sh.pts.Length-2],vy=tmy*sh.pts[sh.pts.Length-1],x,y;
          int rx=vx*tx-vy*ty+ShapeHalf,ry=vx*ty+vy*tx;
          x=sx+(rx*dx-ry*dy)/ShapeBase;y=sy+(rx*dy+ry*dx)/ShapeBase;
          x0=x1=x;y0=y1=y;
          for(int i=0;i<sh.pts.Length;i+=2) {
            vx=tmx*sh.pts[i];vy=tmy*sh.pts[i+1];
            rx=vx*tx-vy*ty+ShapeHalf;ry=vx*ty+vy*tx;
            int nx=sx+(rx*dx-ry*dy)/ShapeBase,ny=sy+(rx*dy+ry*dx)/ShapeBase;
            bool m=m=sh.move!=null&&sh.move[i/2];
            if(!m) {
              if(xor) {
                GDI.MoveToEx(hdc,SX(x,y),SY(x,y),IntPtr.Zero);
                GDI.LineTo(hdc,SX(nx,ny),SY(nx,ny));
                //gr.DrawLine(Pens.Black,IX(x),IY(y),IX(nx),IY(ny));
              } else {path.Add(nx);path.Add(ny);}//DrawLine(x,y,nx,ny,color,draw_brush,false,false);
            }  
            x=nx;y=ny;
            if(x<x0) x0=x;else if(x>x1) x1=x;
            if(y<y0) y0=y;else if(y>y1) y1=y;
          }
          if(xor) {
            GDI.SetROP2(hdc,rop2);
            gr.ReleaseHdc(hdc);
            gr.Dispose();
          } else {
            if(fill>=0) FillPath(path,fill);
            if(color>=0&&(Board.DrawStroke.Checked||fill<0)) {
              float dashoff=0;
              for(int i=2;i<path.Count;i+=2) 
                DrawLine(path[i-2],path[i-1],path[i],path[i+1],color,draw_brush,false,xor,0,Dash,ref dashoff);
              DrawLine(path[path.Count-2],path[path.Count-1],path[0],path[1],color,draw_brush,false,xor,0,Dash,ref dashoff);
            }
            Repaint(x0-1,y0-1,x1+1,y1+1,draw_brush);
          }
        }
        void RepeatDrawEdge(int x0,int y0,int x1,int y1,int color,bmap draw_brush,string shape,int edge,int edge2,int rotate,bool mirrorx,bool mirrory,bool xor,int fill) {
          //Snap(ref x0,ref y0);Snap(ref x1,ref y1);
          for(int r=0;r<RepeatCount();r++) {
            int rx0,ry0,rx1,ry1,rf;
            rf=Repeat(r,x0,y0,out rx0,out ry0);
            Repeat(r,x1,y1,out rx1,out ry1);
            DrawEdge(rx0,ry0,rx1,ry1,color,draw_brush,shape,edge,edge2,rotate,mirrorx,mirrory,xor,fill);
          }
        }
        void DrawEdge(int x0,int y0,int x1,int y1,int color,bmap draw_brush,string shape,int edge,int edge2,int rotate,bool mirrorx,bool mirrory,bool xor,int fill) {
          int r,sx=x0,sy=y0,dx=x1-sx,dy=y1-sy,tx=0,ty=0,tmx=mirrorx?-1:1,tmy=mirrory?-1:1;
          if(dx==0&&dy==0) return;
          Shape sh=ParseShape(shape);
          if(sh==null||sh.pts.Length<4) return;
          edge=bmap.modp(edge*2,sh.pts.Length);
          edge2=bmap.modp(edge2*2,sh.pts.Length);
          if(edge==edge2) edge2=bmap.modp(edge+2,sh.pts.Length);
          switch(rotate&3) {
           case 0:tx=1;ty=0;break;
           case 1:tx=0;ty=-1;break;
           case 2:tx=-1;ty=0;break;
           case 3:tx=0;ty=1;break;
          }
          Graphics gr=null;
          IntPtr hdc=IntPtr.Zero;
          int rop2=0;
          if(xor) {
            gr=CreateGraphics();
            hdc=gr.GetHdc();
            rop2=GDI.SetROP2(hdc,GDI.R2_NOTXORPEN);
            GDI.SelectObject(hdc,GDI.GetStockObject(GDI.WHITE_PEN));
          }
          List<int> path=xor?null:new List<int>();                    
          int vx=tmx*sh.pts[edge],vy=tmy*sh.pts[edge+1],x,y;
          int px=vx*tx-vy*ty,py=vx*ty+vy*tx;
          vx=tmx*sh.pts[edge2];vy=tmy*sh.pts[edge2+1];
          int ex=vx*tx-vy*ty,ey=vx*ty+vy*tx;
          ex-=px;ey-=py;
          int er2=ex*ex+ey*ey;
          vx=tmx*sh.pts[sh.pts.Length-2];vy=tmy*sh.pts[sh.pts.Length-1];
          int rx=vx*tx-vy*ty-px,ry=vx*ty+vy*tx-py;
          int wx=rx*ex+ry*ey,wy=-rx*ey+ry*ex;
          x=sx+(wx*dx-wy*dy)/er2;y=sy+(wx*dy+wy*dx)/er2;
          x0=x1=x;y0=y1=y;
          for(int i=0;i<sh.pts.Length;i+=2) {
            vx=tmx*sh.pts[i];vy=tmy*sh.pts[i+1];
            rx=vx*tx-vy*ty-px;ry=vx*ty+vy*tx-py;
            wx=rx*ex+ry*ey;wy=-rx*ey+ry*ex;
            int nx=sx+(wx*dx-wy*dy)/er2,ny=sy+(wx*dy+wy*dx)/er2;
            bool m=m=sh.move!=null&&sh.move[i/2];
            if(xor) {
              GDI.MoveToEx(hdc,SX(x,y),SY(x,y),IntPtr.Zero);
              GDI.LineTo(hdc,SX(nx,ny),SY(nx,ny));
              //gr.DrawLine(Pens.Black,IX(x),IY(y),IX(nx),IY(ny));
            } else {path.Add(nx);path.Add(ny);}//DrawLine(x,y,nx,ny,color,draw_brush,false,false);
            x=nx;y=ny;
            if(x<x0) x0=x;else if(x>x1) x1=x;
            if(y<y0) y0=y;else if(y>y1) y1=y;
          }                    
          if(xor) {
            GDI.SetROP2(hdc,rop2);
            gr.ReleaseHdc(hdc);
            gr.Dispose();
          } else {
            if(fill>=0) FillPath(path,fill);
            if(color>=0&&(Board.DrawStroke.Checked||fill<0)) {
              float dashoff=0;
              for(int i=2;i<path.Count;i+=2) 
                DrawLine(path[i-2],path[i-1],path[i],path[i+1],color,draw_brush,false,xor,0,Dash,ref dashoff);
              DrawLine(path[path.Count-2],path[path.Count-1],path[0],path[1],color,draw_brush,false,xor,0,Dash,ref dashoff);   
            }
            Repaint(x0-1,y0-1,x1+1,y1+1,draw_brush);          
          }
        }
        void DrawMorph() {
          PushUndo();
          for(int i=0;i<Morph.Length;i++) Morph[i]+=1;
          map.Morph(undomap,Morph);
          for(int i=0;i<Morph.Length;i++) Morph[i]-=1;
        } 
        void DrawSelection() {
          if(Selection[0]<=Selection[2]) DrawRect(Selection[0],Selection[1],Selection[2],Selection[3],bmap.White,DrawBrush,null,0,ShapeMirrorX,ShapeMirrorY,ShapeAdjust,true,-1);
        }
        void CancelSelection() {
          DrawSelection();
          Selection[0]=1;Selection[2]=0;
        }
        void NormSelection() { R.Norm(Selection);}
        bool InSelection(int x,int y) {
          return x>=Selection[0]&&x<=Selection[2]&&y>=Selection[1]&&y<=Selection[3];
        }
        bool InSelection(int x,int y,bool doy) {
          return doy?y>=Selection[1]&&y<=Selection[3]:x>=Selection[0]&&x<=Selection[2];
        }
        bool InSelection(int x,int y,int limit) {
          return y>=Selection[1]+limit&&y<=Selection[3]-limit&&x>=Selection[0]+limit&&x<=Selection[2]-limit;
        }
        bool OutSelection(int x,int y,out int dx,out int dy) {          
          dx=x<Selection[0]?x-Selection[0]:x>Selection[2]?x-Selection[2]:0;
          dy=y<Selection[1]?y-Selection[1]:y>Selection[3]?y-Selection[3]:0;
          return (dy<0?-dy:dy)>(dx<0?-dx:dx);
        }
        MouseButtons MouseOpButtons(MouseOp op) {
          return (LBop==op?MouseButtons.Left:0)|(RBop==op?MouseButtons.Right:0)|(MBOp==op?MouseButtons.Middle:0);
        }        
        bool IsMouseOp(MouseOp op) { return MouseButtons.None!=MouseOpButtons(op);}
        bool IsSelectionMode() {
          return MouseButtons.None!=MouseOpButtons(MouseOp.Select);
        }
        bool IsSelecting() {
          return mop==MouseOp.Select;
        }
        bool IsShaping() {
          return mop==MouseOp.DrawPolar||mop==MouseOp.DrawRect||mop==MouseOp.DrawEdge;
        }
        bool IsSelectionEmpty() {
          return !IsSelectionMode()||Selection[0]>Selection[2];
        }
        bool InSelection() {
          if(IsSelectionEmpty()) return false;
          return R.Inside(Selection,IX(lmx,lmy),IY(lmx,lmy));
        }
        int[] ClippedSelection() {
          if(IsSelectionEmpty()) return null;
          int[] sel2=Selection.Clone() as int[];
          return R.Intersect(sel2,0,0,bm.Width-1,bm.Height-1)?sel2:null;
        }
        int[] ClippedSelectionOrAll() {
          int[] r=ClippedSelection();
          return r!=null?r:new int[] {0,0,bm.Width-1,bm.Height-1};
        }
        Bitmap SelectionBitmap() {
          int[] cs=ClippedSelection();
          if(cs==null) return null;
          Bitmap ret=new Bitmap(cs[2]-cs[0]+1,cs[3]-cs[1]+1);
          bmap.ToBitmap(map,cs[0]+1,cs[1]+1,ret,0,0,ret.Width-1,ret.Height-1,false);
          return ret;
        }
				void MoveSelPoint(int dx,int dy,bool inv) {
				  int ix=IX(lmx,lmy),iy=IY(lmx,lmy);
					bool left=inv^2*ix<Selection[0]+Selection[2],top=inv^2*iy<Selection[1]+Selection[3];
					DrawSelection();
					Selection[left?0:2]+=dx;
					Selection[top?1:3]+=dy;
					R.Norm(Selection);
					DrawSelection();
          if(IsStatusBar()) UpdateStatusBar();
				}
        void KeyMove(int dx,int dy,bool shift,bool alt,bool ctrl) {
          if(alt) SetStatusBar(true);
          if(MovePaste&&!IsSelectionEmpty()) {
            MoveSelection(false,dx,dy,false,false);
            return;
          }
          int z=zoom<ZoomBase?1:zoom/ZoomBase;
          int d=alt?z:shift?10*z:100;
          sx-=d*dx;sy-=d*dy;
          UpdateStatusBar();
          Repaint(false);
        }
        void MoveInMop(bool shift,bool ctrl,int dx,int dy) {
          if(ctrl) {dx*=10;dy*=10;}
          if(shift) {
            DrawXOR();
            pmcx+=dx;pmcy+=dy;
            pmx=SX(pmcx,pmcy);pmy=SY(pmcx,pmcy);
            DrawXOR();
          }
          if(!shift) {
            int mx=dx*zoom/ZoomBase,my=dy*zoom/ZoomBase;
            GDI.POINT xy;
            if(GDI.MoveCursor(mx==0&&dx!=0?dx<0?-1:1:mx,my==0&&dy!=0?dy<0?-1:1:my,out xy)) {
              Point p=PointToClient(Control.MousePosition);
              fMain_MouseMove(null,new MouseEventArgs(MouseButtons,0,p.X,p.Y,0));
            }
          }
        }
        public void UpdatePaste() {
          if(!MovePaste) return;
          if(IsSelectionMode()) DrawSelection();
          R.Shift(Selection,1,1);
          map.CopyRectangle(undomap,Selection[0],Selection[1],Selection[2],Selection[3],Selection[0],Selection[1],-1);
          map.CopyBitmap(MoveBits,Selection[0],Selection[1],MoveTrColor,Board.pasteTRX.Checked,Board.GetDiff(),Board.GetMix(),Board.GetPasteFilter());
          R.Shift(Selection,-1,-1);
          Repaint(Selection[0],Selection[1],Selection[2],Selection[3],true);
          if(IsSelectionMode()) DrawSelection();
        }
        void MoveSelection(bool mouse,int dx,int dy,bool bgcolor,bool trcolor) {
          if(dx==0&&dy==0) {
            DrawSelection();
            return;
          }
          if(!mouse&&mop!=MouseOp.None) {
            MoveInMop(GDI.ShiftKey,false,dx,dy);
            return;
          }
          if(IsSelectionEmpty()) return;
          if(IsSelectionMode()&&!mouse) DrawSelection();
					R.Shift(Selection,1,1);

					bool cs=GDI.CapsLock;
          int[] sel3=null;
          if(MovePaste) {
            if(MoveBits!=null) {              
              map.CopyRectangle(undomap,Selection[0],Selection[1],Selection[2],Selection[3],Selection[0],Selection[1],-1);
              sel3=map.CopyBitmap(MoveBits,Selection[0]+dx,Selection[1]+dy,MoveTrColor,Board.pasteTRX.Checked,Board.GetDiff(),Board.GetMix(),Board.GetPasteFilter());
              if(MoveXor||MoveBits!=null) sel3=null;
              else R.Shift(sel3,-1,-1);              
            } else
              map.MoveRectangle(Selection[0],Selection[1],Selection[2],Selection[3],Selection[0]+dx,Selection[1]+dy,undomap);
          } else {
            PushUndo("Move");
            map.MoveRectangle(Selection[0],Selection[1],Selection[2],Selection[3],Selection[0]+dx,Selection[1]+dy,bgcolor?-1:Color2,trcolor?Color2:-1,cs);            
						if(bgcolor&&!cs) {
						  if(dy==0&&dx<-1)
							  for(int i=-1;i>dx;i--) map.MoveRectangle(Selection[2],Selection[1],Selection[2],Selection[3],Selection[2]+i,Selection[1],-1,-1,false);
						  if(dy==0&&dx>1)
							  for(int i=1;i<dx;i++) map.MoveRectangle(Selection[0],Selection[1],Selection[0],Selection[3],Selection[0]+i,Selection[1],-1,-1,false);
						  if(dx==0&&dy<-1)
							  for(int i=-1;i>dy;i--) map.MoveRectangle(Selection[0],Selection[3],Selection[2],Selection[3],Selection[0],Selection[3]+i,-1,-1,false);
						  if(dx==0&&dy>1)
							  for(int i=1;i<dy;i++) map.MoveRectangle(Selection[0],Selection[1],Selection[2],Selection[1],Selection[0],Selection[1]+i,-1,-1,false);
						}
          }
					R.Shift(Selection,-1,-1);
          int[] sel2=Selection.Clone() as int[];
          if(sel3!=null) Selection=sel3; 
          else {
            Selection[0]+=dx;Selection[1]+=dy;Selection[2]+=dx;Selection[3]+=dy;
          }
          if(R.Intersected(Selection,sel2[0],sel2[1],sel2[2],sel2[3])||!MovePaste&&(cs||bgcolor))
            R.Union(sel2,Selection[0],Selection[1],Selection[2],Selection[3]);
          else {
            Repaint(Selection[0],Selection[1],Selection[2],Selection[3],true);
          }
          Repaint(sel2[0],sel2[1],sel2[2],sel2[3],true);
          if(IsSelectionMode()) DrawSelection();
          lmx=SX(sel2[0],sel2[1]);lmy=SY(sel2[0],sel2[1]);
          UpdateStatusBar();
        }
        static void Polyline(Graphics gr,Pen p,bool q,int sx,int sy,params int[] xy) {
          Point[] pt=new Point[xy.Length/2];          
          int x=q?sy:sx,y=q?sx:sy;
          for(int i=0;i<pt.Length;i++) {
            pt[i].X=x+xy[2*i+(q?1:0)];
            pt[i].Y=y+xy[2*i+(q?0:1)];
          }
          gr.DrawLines(p,pt);
        }
        static void Ellipse(Graphics gr,Pen p,bool q,int x,int y,int w,int h) {
          int r;
          if(q) {r=x;x=y;y=r;r=w;w=h;h=r;}
          gr.DrawEllipse(p,x,y,w,h);
        }
        static void Line(Graphics gr,Pen p,bool q,int x,int y,int x2,int y2) {
          int r;
          if(q) {r=x;x=y;y=r;r=x2;x2=y2;y2=r;}
          gr.DrawLine(p,x,y,x2,y2);
        }
        static void Rectangle(Graphics gr,Pen p,bool q,int x,int y,int w,int h) {
          int r;
          if(q) {r=x;x=y;y=r;r=w;w=h;h=r;}
          gr.DrawRectangle(p,x,y,w,h);
        }
        static void Arc(Graphics gr,Pen p,bool q,int x,int y,int w,int h,int start,int sweep) {
          int r;
          if(q) {r=x;x=y;y=r;r=w;w=h;h=r;start=90-start;sweep=-sweep;}
          gr.DrawArc(p,x,y,w,h,start,sweep);
        }
        static void Rounded(Graphics gr,Pen p,bool q,float x,float y,float w,float h,float r) {
          float f;
          if(q) {f=x;x=y;y=f;f=w;w=h;h=f;}
          DrawRounded(gr,p,null,x,y,w,h,new float[] {r},false);
        }
        void Paper(bool q,int penwidth,int color,int bg,int mode) {
          int qi=q?1:0,x,y,x2,y2,sx=Selection[qi],sy=Selection[1-qi],w=Selection[2+qi]-sx+1,h=Selection[3-qi]-sy+1,sw=q?bm.Height:bm.Width,sh=q?bm.Width:bm.Height;
          if(w<2||h<2||w<=penwidth+1||h<=penwidth+1) {
            sx=sy=0;w=h=40;
          }
          PushUndo();
          using(Graphics gr=Graphics.FromImage(bm)) {
            if(bg>=0&&bg!=color)
              gr.FillRectangle(bg==bmap.White?Brushes.White:new SolidBrush(Color.FromArgb(bg|~0xffffff)),0,0,bm.Width,bm.Height);
            Pen p=new Pen(Color.FromArgb(color|~0xffffff),penwidth);
            if(mode!=0) {
              if(mode==13) {
                int r=(int)Math.Sqrt(sw*sw+sh*sh);
                for(y=0;y*y<sw*sw+sh*sh;y+=h)
                  Ellipse(gr,p,q,sx-y,sy-y,2*y-1,2*y-1);
                for(x=0;x<12*32;x++) {
                  double a=x*2*Math.PI/12/32,co=Math.Cos(a),si=Math.Sin(a),r2;
                  int m=(x&1)!=0?16:(x&2)!=0?8:(x&4)!=0?4:(x&8)!=0?2:(x&16)!=0?1:0;
                  r2=h*(int)(3*m*w/h);
                  if(r2>=r) continue;
                  Line(gr,p,q,(int)Math.Round(sx+r2*co),(int)Math.Round(sy+r2*si),(int)Math.Round(sx+r*co),(int)Math.Round(sy+r*si));
                }                
              } else {
               int xmax=bm.Width;
               if(mode==21||mode==22) xmax+=w/2;
               for(y=sy%h-h,y2=0;y<sh;y+=h,y2++) 
                for(x=sx%w-w,x2=0;x<xmax;x+=w,x2++) {
                  if(mode==1||mode==2) Polyline(gr,p,q,x,y,0,h/2,w/2,0,w,h/2,w/2,h,0,h/2);
                  else if(mode==3||mode==4) {
                    int d=w/4;
                    if(d>h/2) d=h/3;
                    if((y2&1)!=0) {
                      Polyline(gr,p,q,x,y,0,0,0,h-d,w/2,h,w,h-d,w,0);
                      if(mode==4) {
                        Polyline(gr,p,q,x,y,w,0,w/2,d,w/2,h);
                        Polyline(gr,p,q,x,y,0,0,w/2,d);
                      }
                    } else {
                      Polyline(gr,p,q,x,y,0,h,w/2,h-d,w,h);
                      Polyline(gr,p,q,x,y,w/2,h-d,w/2,0);
                      if(mode==4)
                        Polyline(gr,p,q,x,y,w/2,0,0,d,w/2,0,w,d,w,h);
                    }                    
                  } else if(mode==5) {
                    Polyline(gr,p,q,x,y,0,0,w,h);
                    Polyline(gr,p,q,x,y,w,0,0,h);
                  } else if(mode==6||mode==16) {
                    int dx=w/4,dy=h/4;
                    if(dx<dy) dy=dx;else dx=dy;
                    Rectangle(gr,p,q,x+dx,y+dy,w-2*dx,h-2*dy);
                  } else if(mode==7||mode==17) {
                    Ellipse(gr,p,q,x,y,w,h);
                  } else if(mode==8) {
                    int d=h/12;
                    if((y2&1)!=0) Ellipse(gr,p,q,x,y-d,w,h+2*d);
                    else Ellipse(gr,p,q,x+w/2,y-d,w,h+2*d);
                  } else if(mode==9) {
                    Ellipse(gr,p,q,x-w,y-h,2*w,2*h);
                  } else if(mode==10) {
                    Polyline(gr,p,q,x,y,0,0,w,0);
                    if((y2&1)!=0) Polyline(gr,p,q,x,y,0,0,w/2,h,w,0);
                    else Polyline(gr,p,q,x,y,0,h,w/2,0,w,h);
                  } else if(mode==11) {
                    int d=(w<h?w:h)/4;
                    Polyline(gr,p,q,x,y,d,0,w-d,0,w,d,w,h-d,w-d,h,d,h,0,h-d,0,d,d,0);
                  } else if(mode==12) {
                    int d=(w<h?w:h)/4;
                    if((y2&1)==(x2&1)) {
                      Rectangle(gr,p,q,x+d,y+d,w-2*d,h-2*d);
                      Polyline(gr,p,q,x,y,d,d,-d,-d);
                      Polyline(gr,p,q,x,y,w-d,d,w+d,-d);
                    }
                  } else if(mode==14) {
                    float r=(w<h?w:h)/5;
                    Rounded(gr,p,q,x,y,w,h,r);
                  } else if(mode==15) {
                    float r=(w<h?w:h)/5,p2=penwidth;
                    Rounded(gr,p,q,x+p2,y+p2,w-2*p2,h-2*p2,r);
                  } else if(mode==17) {
                    Ellipse(gr,p,q,x,y,w,h);
                  } else if(mode==18) {
                    int dx=w/8,dy=h/8;
                    if(dx<dy) dy=dx;else dx=dy;                    
                    Line(gr,p,q,x+w/2,y-dy,x+w/2,y+3*dy);
                    Line(gr,p,q,x+w/4,y+2*dy,x+3*w/4,y+2*dy);
                    Rectangle(gr,p,q,x+dx,y+3*dy,w-2*dx,h-4*dy);                    
                  } else if(mode==19) {
                    int dx=w/4,dy=h/4;
                    if(dx<dy) dy=dx;else dx=dy;
                    Rectangle(gr,p,q,x+dx,y+dy,w-2*dx,h-2*dy);
                    Line(gr,p,q,x+w/2,y-dy,x+w/2,y+dy);
                    Line(gr,p,q,x-dx,y+h/2,x+dx,y+h/2);
                  } else if(mode==20) {
                    bool even=(y2&1)==0;
                    Line(gr,p,q,x,y,x+w,y);
                    if(even) Line(gr,p,q,x+w/2,y,x+w/2,y+h);
                    else Line(gr,p,q,x,y,x,y+h);
                  } else if(mode==21) {
                    bool ver=h>2*w?true:w>2*h?false:true,even=((ver?y2:x2)&1)==0;                    
                    if(ver) {
                      int d=h-w,dx=even?0:-w/2;
                      Line(gr,p,q,x+dx,y,x+dx,y+d+w/2);
                      Arc(gr,p,q,x+dx,y+d,w,w,0,180);
                    } else {
                      int d=w-h,dy=even?0:-h/2;
                      Line(gr,p,q,x,y+dy,x+d+h/2,y+dy);
                      Arc(gr,p,q,x+d,y+dy,h,h,270,180);
                    }
                  } else if(mode==22) {
                    bool ver=h>2*w?true:w>2*h?false:true,even=((ver?y2:x2)&1)==0;                    
                    if(ver) {
                      int d=h-w,dx=even?0:-w/2;
                      Line(gr,p,q,x+dx,y,x+dx,y+d+w/2);
                      Line(gr,p,q,x+dx+w/2,y+h,x+dx,y+d+w/2);
                      Line(gr,p,q,x+dx+w/2,y+h,x+dx+w,y+d+w/2);
                    } else {
                      int d=w-h,dy=even?0:-h/2;
                      Line(gr,p,q,x,y+dy,x+d+h/2,y+dy);
                      Line(gr,p,q,x+w,y+dy+h/2,x+d+h/2,y+dy);
                      Line(gr,p,q,x+w,y+dy+h/2,x+d+h/2,y+dy+h);                      
                    }
                  }
                }
              }
            }
            if(mode==2) for(x=sx%w-w/2;x<sw;x+=w)
              Line(gr,p,q,x,0,x,bm.Height);
            if(mode>=23&&mode<=26) {
              int m=mode==26?6:mode-19;
              if(w>3) for(x=sx%(m*w);x<sw;x+=m*w) {
                Line(gr,p,q,x-1,0,x-1,sh);
                Line(gr,p,q,x+1,0,x+1,sh);
              }
              if(mode==26) m=4;
              if(h>3) for(y=sy%(m*h);y<sh;y+=m*h) {
                Line(gr,p,q,0,y-1,sw,y-1);
                Line(gr,p,q,0,y+1 ,sw,y+1);
              }
              mode=23;
            }
            if(w>2&&(mode==0||mode==2||mode==5||mode==6||mode==7||mode==9||mode==23))
              for(x=sx%w;x<sw;x+=w)
                Line(gr,p,q,x,0,x,sh);
            if(h>2&&(mode==0||mode==5||mode==6||mode==7||mode==9||mode==23))
              for(y=sy%h;y<sh;y+=h)
                Line(gr,p,q,0,y,sw,y);
          }
          bmap.FromBitmap(map,bm,-1);
          Repaint(true);
        }
        void Replace(string ucode,int search,int replace,int mode,int level,int grad) {
          if(search==-1) search=map.XY(IX(lmx,lmy)+1,IY(lmx,lmy)+1);          
          if(search==-1||search==replace) return;
          PushUndo(false,ucode);
          int x0,y0,x1,y1;
          if(IsSelectionEmpty()) {x0=y0=0;x1=bm.Width-1;y1=bm.Height-1;}
          else {x0=Selection[0];y0=Selection[1];x1=Selection[2];y1=Selection[3];}
          if(grad>0) {
            if(grad==4) map.ReplaceCirc(search,Color1,Color2,mode,x0+1,y0+1,x1+1,y1+1);
            else map.ReplaceFill(search,Color1,Color2,mode,grad,x0+1,y0+1,x1+1,y1+1);
          } else if(level==0) map.Replace(search,replace,x0+1,y0+1,x1+1,y1+1);
          else map.ReplaceDiff(search,replace,mode,level,x0+1,y0+1,x1+1,y1+1);
          Repaint(true);
        }
        void RemoveColor(int color,bool x8,bool repeat) {
          PushUndo(false);
          int[] r=Rect();
          map.RemoveColor(color,x8,repeat,r[0],r[1],r[2],r[3]);
          Repaint(true);
        }
        void EraseColor(bool vert,bool hori) {
          PushUndo(false);
          int[] r=Rect();
          map.EraseColor(r[0],r[1],r[2],r[3],Color1,vert,hori);
          Repaint(true);
        }
        int OnDeleteRect(object par,bmap map,int x,int y,bmap search) {
          object[] oa=par as object[];
          if(oa[0]!=null) {
            StringBuilder sb=oa[0] as StringBuilder;
            string dxy="";
            if(oa[2]!=null) {
              int px=(int)oa[2],py=(int)oa[3],dx=search.Width-px,dy=search.Height-py,dx2=px,dy2=py,c,p,pe;
              if(x+search.Width<map.Width-1) {
                dx++;
                pe=(y+py)*map.Width;p=pe+x+search.Width+1;pe+=map.Width-1;
                c=map.Data[p-1];
                for(;p<pe&&map.Data[p]==c;dx++,p++);
              }
              if(x>1) {
                dx2++;
                pe=y*map.Width;p=pe+x-2;
                c=map.Data[p+1];
                for(;p>pe&&map.Data[p]==c;dx2++,p--);
              }
              if(y+search.Height<map.Height-1) {
                dy++;
                pe=x+px;p=pe+(y+search.Height+1)*map.Width;pe+=(map.Height-1)*map.Width;c=map.Data[p-map.Width];
                for(;p<pe&&map.Data[p]==c;dy++,p+=map.Width);
              }
              if(y>1) {
                dy2++;
                pe=x;p=pe+(y-2)*map.Width;
                c=map.Data[p+map.Width];
                for(;p>pe&&map.Data[p]==c;dy2++,p-=map.Width);
              }
              dxy="\t"+dx+"\t"+dy+"\t"+dx2+"\t"+dy2;
            }
            sb.AppendLine(""+x+'\t'+y+dxy);
          }
          if(oa[1]!=null) {
            Bitmap bm=oa[1] as Bitmap;
            map.CopyBitmap(bm,x,y,Board.pasteTrans.Checked?Color2:-1,Board.pasteTRX.Checked,Board.GetDiff(),Board.GetMix(),Board.GetPasteFilter());
            return 1;
          }
          return 0;
        }
        fillres DeleteRect(bool ctrl,bool trans) {
          int[] sel=R.Copy(Selection);
          if(!R.Intersect(sel,0,0,bm.Width-1,bm.Height-1)) return new fillres();
          PushUndo(false);
          bool over=Board.chSearchOver.Checked; 
          fillres fr;
          if(R.IsPoint(sel)) {
            int color=map.XY(sel[0]+1,sel[1]+1);
            fr=map.Replace(color&bmap.White,over?0:trans?bmap.White:Color1,1,1,map.Width-1,map.Height-1);
          } else {
            bmap search=new bmap(sel[2]-sel[0]+1,sel[3]-sel[1]+1);
            search.CopyRectangle(map,sel[0]+1,sel[1]+1,sel[2]+1,sel[3]+1,0,0,-1);
            object[] oa=null;
            bool b0,b1=false;
            if((b0=Board.chSearchCopy.Checked)||(b1=Board.chSearchPaste.Checked)) {
              oa=new object[4];
              if(b0) {
                oa[0]=new StringBuilder();
                int px=IX(lmx,lmy)-sel[0],py=IY(lmx,lmy)-sel[1];
                if(px>=0&&px<search.Width&&py>=0&&py<search.Height) {
                  oa[2]=px;oa[3]=py;
                }
              }
              if(b1) oa[1]=Clipboard.GetImage() as Bitmap;
            }
            int cmode=Board.GetReplace('S');
            fr=map.SearchRectangle(undomap,search,0,0,search.Width,search.Height,over,cmode>0?Color1:Color2,trans?map.XY(IX(lmx,lmy)+1,IY(lmx,lmy)+1):-1,cmode,oa!=null?new bmap.DelegateSearch(OnDeleteRect):null,oa);
            if(b0) try {string t=""+oa[0];if(t!="") Clipboard.SetText(t);} catch {};
          }
          if(!GDI.NumLock) {
            DelTotal+=fr.m; 
            if(DialogResult.Yes==MessageBox.Show(null,""+fr.m+(DelTotal>fr.m?"/"+DelTotal:""),"Search count, reset counter ?",MessageBoxButtons.YesNo,MessageBoxIcon.Information,MessageBoxDefaultButton.Button2))
              DelTotal=0;
          }
          Repaint(true);
          return fr;
        }
        void FindRect(bool backward,bool exact) {
          int x=Selection[0]+1,y=Selection[1]+1;
          Cursor old=this.Cursor;
          this.Cursor=Cursors.WaitCursor;
          long delta=map.FindRectangle(ref x,ref y,Selection[2],Selection[3],backward,exact?0:16);
          this.Cursor=old;
          if(x<0) return;          
          DrawXOR();
          R.Shift(Selection,x-1-Selection[0],y-1-Selection[1]);
          DrawXOR();
        }
        void Duplicate(bool mirror,bool brick) {          
          int x=IX(lmx,lmy),y=IY(lmx,lmy),w=Selection[2]-Selection[0]+1,h=Selection[3]-Selection[1]+1,dx=bmap.idiv(x-Selection[0],w),dy=bmap.idiv(y-Selection[1],h),ddx=dx<0?-1:1,ddy=dy<0?-1:1;
					if(dx==0&&dy==0) {Erase(true);return;}
          PushUndo();
          bool xbrick=brick&&mirror,ybrick=brick&&!mirror;
          if(brick) mirror=false;
          for(int j=0;j!=(dy+ddy);j+=ddy) {
            for(int i=0;i!=(dx+ddx);i+=ddx) 
              if(i!=0||j!=0) {
                int nx=1+Selection[0]+w*i,ny=1+Selection[1]+h*j;
                if(ybrick&&0!=(j&1)) {
                  int w2=w/2;
                  map.CopyRectangle(map,Selection[0]+1,Selection[1]+1,Selection[2]+1-w2,Selection[3]+1,nx+w2,ny,-1);
                  if(w2>0) map.CopyRectangle(map,Selection[0]+1+w-w2,Selection[1]+1,Selection[2]+1,Selection[3]+1,nx,ny,-1);
                } else if(xbrick&&0!=(i&1)) {
                  int h2=h/2;
                  map.CopyRectangle(map,Selection[0]+1,Selection[1]+1,Selection[2]+1,Selection[3]+1-h2,nx,ny+h2,-1);
                  if(h2>0) map.CopyRectangle(map,Selection[0]+1,Selection[1]+1+h-h2,Selection[2]+1,Selection[3]+1,nx,ny,-1);
                } else 
                  map.CopyRectangle(map,Selection[0]+1,Selection[1]+1,Selection[2]+1,Selection[3]+1,nx,ny,-1);
                if(mirror&&!brick) map.Mirror(0!=(j&1),0!=(i&1),nx,ny,nx+w-1,ny+h-1);
              }
          }
          Selection[0]+=dx*w;Selection[1]+=dy*h;Selection[2]+=dx*w;Selection[3]+=dy*h;
          Repaint(true);
        }
        public void SetFillPattern(bool flip,bool off) {
          if(flip) { Pattern.Enabled=Pattern.BMap!=null&&!Pattern.Enabled;return;}
          if(off) { Pattern.Enabled=false;return;}
          int[] patt=Selection.Clone() as int[];
          if(!R.Intersect(patt,0,0,bm.Width-1,bm.Height-1)||R.IsPoint(patt)) return;
          Pattern.X=IX(lmx,lmy);Pattern.Y=IY(lmx,lmy);
          Pattern.BMap=new bmap(R.Width(patt),R.Height(patt));
          Pattern.BMap.CopyRectangle(map,patt[0]+1,patt[1]+1,patt[2]+1,patt[3]+1,0,0,-1);
          Pattern.Enabled=true;
        }
        void CopySelection() {
          NormSelection();
          if(Selection[0]==-1) return;
          int[] sel2=Selection.Clone() as int[];
          if(!R.Intersect(sel2,0,0,bm.Width-1,bm.Height-1)) return;
          Bitmap part=bm.Clone(new Rectangle(sel2[0],sel2[1],sel2[2]-sel2[0]+1,sel2[3]-sel2[1]+1),bm.PixelFormat);
          if(Board.chCopyPNG.Checked) {
            part.MakeTransparent(IntColor(Board.GetIconTrColor(true)));
            DataObject data=new DataObject();
            MemoryStream ms=new MemoryStream();
            part.Save(ms,ImageFormat.Png);
            data.SetData("PNG",false,ms);
            Clipboard.Clear();
            Clipboard.SetDataObject(data,true);
            ms.Dispose();
          } else
            Clipboard.SetImage(part);
          part.Dispose();
        }
        Bitmap ResizeImage(Image x,int width,int height) {
          Bitmap res=new Bitmap(width,height);
          using(Graphics gr=Graphics.FromImage(res)) {
            gr.CompositingMode=CompositingMode.SourceCopy;
            gr.PixelOffsetMode=PixelOffsetMode.Half;
            //gr.InterpolationMode=InterpolationMode.NearestNeighbor;
            //gr.DrawImage(x,0,0,width,height);
            gr.InterpolationMode=InterpolationMode.HighQualityBilinear;
            gr.SmoothingMode=SmoothingMode.HighQuality;
            gr.DrawImage(x,0,0,width,height);
          }
          return res;
        }        
        void PasteSelection(Bitmap cli,int trcolor,bool trx,int diff,bool repeat,bool quad,bool extend,int mix,int filter,int append) {
          if(cli==null) {
            cli=Clipboard.GetImage() as Bitmap;
            if(cli==null) {
              string text=Clipboard.GetText();
              if(""+text!="") {
                bool line=Board.chTPasteLine.Checked,clear=false;
                int lf;
                if(line) {
                  if(0<=(lf=text.IndexOf('\n'))) {
                    if(lf+1<text.Length) Clipboard.SetText(text.Substring(lf+1));else Clipboard.Clear();
                    if(lf>0&&text[lf-1]=='\r') lf--;
                    text=text.Substring(0,lf);
                  } else
                    Clipboard.Clear();
                }
                if(Board.pasteFiles.Checked) {
                  if(IsFile(text,true)) cli=Bitmap.FromFile(text) as Bitmap;
                  else if(IsUrl(text)) cli=LoadUrl(text);
                }
                if(cli==null) {
                  string[] sa=text.Replace("\r\n","\n").Split('\t','\n');
                  text=string.Join("\n",sa);
                  Board.tbText.Text=text;
                  DrawText(GDI.ShiftKey?GDI.ShiftRKey?1:-1:0,0,IX(lmx,lmy),IY(lmx,lmy));
                  return;
                }
                
              }
              MemoryStream ms=Clipboard.GetData("PNG") as MemoryStream;
              if(ms!=null) {
                cli=Image.FromStream(ms) as Bitmap;
              }
            }
          }
          if(cli==null||cli.Width<1||cli.Height<1) return;
          PushUndo();
          int dx=IX(lmx,lmy),dy=IY(lmx,lmy);
          if(append>0) {
            dx=append==6?-cli.Width:append==2?map.Width-2:0;dy=append==5?-cli.Height:append==1?map.Height-2:0;extend=true;
          } else if(InSelection(dx,dy)) {
            if(repeat) {
              for(int y=Selection[1];y+cli.Height-1<=Selection[3];y+=cli.Height) {
                for(int x=Selection[0];x+cli.Width-1<=Selection[2];x+=cli.Width) {
                  map.CopyBitmap(cli,x+1,y+1,trcolor,trx,diff,mix,filter);
                }
              }
              DrawXOR();Repaint(true);
              NoMovePaste();
              return;
            }
            if(quad) {
              int w2=(cli.Width+1)/2,h2=(cli.Height+1)/2,w3=cli.Width-w2,h3=cli.Height-h2,sx=Selection[0]+1,sy=Selection[1]+1,sx2=Selection[2]+1,sy2=Selection[3]+1,c;
              if(w2+w3>sx2-sx+1) {w2=(sx2-sx)/2;w3=sx2-sx+1-w2;}
              if(h2+h3>sy2-sy+1) {h2=(sy2-sy)/2;h3=sy2-sy+1-h2;}
              w2--;h2--;
              bmap bc=new bmap(cli.Width,cli.Height);
              bc.CopyBitmap(cli,0,0,-1,false,0,0,-1);
              c=bc.XY(w2,h2);
              if(c!=trcolor) map.FillRectangle(sx,sy,sx2,sy2,c);
              map.CopyRectangle(bc,0,0,w2,h2,sx,sy,trcolor);
              map.CopyRectangle(bc,0,cli.Height-h3,w2,cli.Height-1,sx,sy2-h3+1,trcolor);
              for(int x=sx+w2+1;x<sx2-w3+1;x++) {
                map.CopyRectangle(bc,w2,0,w2,h2,x,sy,trcolor);
                map.CopyRectangle(bc,w2,cli.Height-h3,w2,cli.Height-1,x,sy2-h3+1,trcolor);
              }
              for(int y=sy+h2+1;y<sy2-h3+1;y++) {
                map.CopyRectangle(bc,0,h2,w2,h2,sx,y,trcolor);
                map.CopyRectangle(bc,cli.Width-w3,h2,cli.Width-1,h2,sx2-w3+1,y,trcolor);
              }
              map.CopyRectangle(bc,cli.Width-w3,0,bc.Width-1,h2,sx2-w3+1,sy,trcolor);
              map.CopyRectangle(bc,cli.Width-w3,cli.Height-h3,bc.Width-1,cli.Height-1,sx2-w3+1,sy2-h3+1,trcolor);
              DrawXOR();Repaint(true);
              NoMovePaste();
              return;
            }
            extend=true;//dx<0||dy<0||dx>=bm.Width||dy>=bm.Height;
            int w=Selection[2]-Selection[0]+1,h=Selection[3]-Selection[1]+1,wh;
            bool x3=dx>=Selection[0]+w/3&&dx<=Selection[2]-w/3,y3=dy>=Selection[1]+h/3&&dy<=Selection[3]-h/3;
            if(w==1&&h==1) {
              w=cli.Width;h=cli.Height;dx=Selection[0];dy=Selection[1];
            } else if(x3||y3) {
              if(x3&&y3) {
                dx=Selection[0];dy=Selection[1];
                cli=ResizeImage(cli,w,h);
              } else if(x3) {
                wh=w*cli.Height/cli.Width;
                dx=Selection[0];
                dy=dy<Selection[1]+h/2?Selection[1]:Selection[3]-wh+1;
                cli=ResizeImage(cli,w,wh);
              } else {
                wh=h*cli.Width/cli.Height;
                dx=dx<Selection[0]+w/2?Selection[0]:Selection[2]-wh+1;dy=Selection[1];
                cli=ResizeImage(cli,wh,h);
              }
            } else {
              dx=dx-Selection[0]<Selection[2]-dx?Selection[0]:Selection[2]-cli.Width+1;
              dy=dy-Selection[1]<Selection[3]-dy?Selection[1]:Selection[3]-cli.Height+1;
            }
          }
          bmap map2=map;
          if(extend) {
            map=map.Extend(dx,dy,dx+cli.Width+1,dy+cli.Height+1,Color2,true);
            if(map==null) map=map2;
            else {
              if(dx<0) {Selection[0]-=dx;Selection[2]-=dx;sx+=dx*zoom/ZoomBase;dx=0;}
              if(dy<0) {Selection[1]-=dy;Selection[3]-=dy;sy+=dy*zoom/ZoomBase;dy=0;}
            }
          }
          DrawXOR();
          bool movebit=!extend&&(dx<0||dy<0||dx+cli.Width>=bm.Width||dy+cli.Height>=bm.Height)||trcolor>=0||diff>0||bmap.IsAlpha(cli)||mix>0||filter!=-1;
          int[] sel=map.CopyBitmap(cli,dx+1,dy+1,trcolor,trx,diff,mix,filter);
          if(sel!=null) {
            if(movebit) {
              Selection[0]=dx;Selection[1]=dy;Selection[2]=dx+cli.Width-1;Selection[3]=dy+cli.Height-1;
            } else {
              Selection[0]=sel[0]-1;Selection[1]=sel[1]-1;Selection[2]=sel[2]-1;Selection[3]=sel[3]-1;
            }
            if(map==map2) Repaint(Selection[0],Selection[1],Selection[2],Selection[3],true);
          }
          DrawXOR();
          if(map2!=map) {UpdateBitmap();Repaint(true);}
          if(movebit) {MoveBits=cli;MoveTrColor=trcolor;MoveXor=diff>0;}
          MovePaste=true;
        }
        void NoMovePaste() { MovePaste=false;MoveBits=null;}
        private void fMain_MouseDown(object sender,MouseEventArgs e) {
          bool lb=0!=(e.Button&MouseButtons.Left),rb=0!=(e.Button&MouseButtons.Right);
          if(mop==MouseOp.None&&!lb&&!rb&&GDI.ShiftKey&&GDI.CtrlKey) {
            SetAngle(e.X,e.Y,0);
            Repaint(false);
            return;
          }          
          int ex=IX(e.X,e.Y),ey=IY(e.X,e.Y);  
          if(mop!=MouseOp.None) {
            if(e.Button==pmb||e.Button==MouseButtons.Middle) return;
            switch(mop) {
             case MouseOp.Select:SwitchPress();break;
             case MouseOp.FillShape:
             case MouseOp.FillLinear:
             case MouseOp.FillRadial:case MouseOp.FillSquare:
             case MouseOp.Fill4:case MouseOp.FillVect:
             case MouseOp.FillFlood:case MouseOp.FillFloat:
             case MouseOp.FillBorder:{              
              int x=ex,y=ey;
              if(x<0||x>=bm.Width||y<0||y>=bm.Height) break;              
              Array.Resize(ref mopt,mopt==null?2:mopt.Length+2);
              mopt[mopt.Length-2]=x+1;mopt[mopt.Length-1]=y+1;
              } break;
             case MouseOp.DrawFree:
             case MouseOp.DrawLine:{
              Snap(ref ex,ref ey);
              int x=ex,y=ey;
              bool point=x==pmcx&&y==pmcy;
              DrawXOR();
              int nx=point?pmcx2:pmcx,ny=point?pmcy2:pmcy;              
              if(mop==MouseOp.DrawFree) {freex=nx;freey=ny;}              
              RepeatDrawLine(nx,ny,x,y,DrawColor(pmk),DrawBrush,true,false,(GDI.ShiftKey?1:0)|(GDI.CtrlKey?2:0)|(GDI.AltKey?4:0));
              if(mop==MouseOp.DrawLine) {
                pmx=e.X;pmy=e.Y;
                pmcx=ex;pmcy=ey;                
              }
              FinishMop(mop,pmcx,pmcy,x,y);
              DrawXOR();
              } break; 
             case MouseOp.DrawRect:
             case MouseOp.DrawPolar:
             case MouseOp.DrawEdge:
              DrawXOR();
              Array.Resize(ref mopt,2);
              Snap(ref ex,ref ey);
              mopt[0]=ex;mopt[1]=ey;
              DrawXOR();
              break;
             case MouseOp.DrawMorph:
              DrawXOR();
              if(Morphs<8) {
                Morph[2*Morphs]=IX(e.X,e.Y);
                Morph[2*Morphs+1]=IY(e.X,e.Y);
                Morphs++;
                if(Morphs==8) {
                  DrawMorph();
                  Morphs=4;
                  Repaint(true,false);
                }
              }
              DrawXOR();
              break;
            }
            return;
          }
          if(0!=(e.Button&MouseButtons.Left)) mop=LBop;
          else if(0!=(e.Button&MouseButtons.Right)) mop=RBop;
          else if(0!=(e.Button&MouseButtons.Middle)) mop=MBOp;
          if(mop==MouseOp.None) return;
          int cx=ex,cy=ey;
          pmk=(GDI.ShiftKey?1:0)|(GDI.CtrlKey?2:0)|(GDI.AltKey?4:0);
          switch(mop) {
           case MouseOp.Pan:
            break;
           case MouseOp.DrawFree:
           case MouseOp.DrawLine:
           case MouseOp.DrawPolar:
           case MouseOp.DrawRect:
           case MouseOp.DrawEdge:
            PushUndo();
            Snap(ref cx,ref cy);            
            if(mop==MouseOp.DrawFree) {              
              int cx2=cx,cy2=cy;                            
              freex=cx2;freey=cy2;
              for(int r=0;r<RepeatCount();r++) {
                int rx,ry;
                Repeat(r,cx2,cy2,out rx,out ry);
                map.Brush(rx+1,ry+1,DrawColor(pmk),DrawBrush,BrushWhiteOnly);
                int bw=DrawBrush==null?1:DrawBrush.Width,bh=DrawBrush==null?1:DrawBrush.Height;
                Repaint(rx-bw/2-1,ry-bh/2-1,rx+2+bw/2,ry+2+bh/2,true);
              }
            }
            break;
           case MouseOp.DrawMorph:
            Morphs=1;
            Morph[0]=cx;Morph[1]=cy;
            break;
           case MouseOp.Select:
            DrawSelection();
            movesel=((pmk&1)==0)&&InSelection(cx,cy);
            if(movesel)
              DrawSelection();
            else {
              NoMovePaste();
              DrawRect(IX(e.X,e.Y),IY(e.X,e.Y),IX(e.X,e.Y),IY(e.X,e.Y),0!=(pmk&3)?bmap.White:0,DrawBrush,null,0,ShapeMirrorX,ShapeMirrorY,ShapeAdjust,true,-1);
            }
            break;
          }
         lmx=e.X;lmy=e.Y;
         pmx=e.X;pmy=e.Y;pmcx2=pmcx=cx;pmcy2=pmcy=cy;
         pmb=e.Button;
        }
        
        void DrawXOR() { DrawXOR(IX(lmx,lmy),IY(lmx,lmy));}
        void DrawXOR(int cx,int cy) {
          int px=pmcx,py=pmcy;
          if(IsSnap(mop)) {Snap(ref px,ref py);Snap(ref cx,ref cy); }
          if(DrawCenter&&(mop==MouseOp.DrawRect||mop==MouseOp.DrawPolar||mop==MouseOp.DrawEdge)) {px+=pmcx-cx;py+=pmcy-cy;}
         switch(mop) {
          case MouseOp.DrawLine:
           RepeatDrawLine(px,py,cx,cy,0!=(pmk&3)?bmap.White:0,DrawBrush,true,true,0);
           break;
          case MouseOp.DrawPolar:
           if(mopt!=null) {
             int x1=mopt[0],y1=mopt[1],x2=cx,y2=cy;
             if(DrawOrto) RectOrto(pmcx,pmcy,ref x1,ref y1,ref x2,ref y2);                       
             RepeatDrawPara(pmcx-x2+x1,pmcy-y2+y1,x1-x2+x1,y1-y2+y1,x2,y2,0!=(pmk&3)?bmap.White:0,DrawBrush,DrawShape,ShapeRotate,ShapeMirrorX,ShapeMirrorY,ShapeAdjust,true,-1);
           } else RepeatDrawPolar(px,py,cx,cy,0!=(pmk&3)?bmap.White:0,DrawBrush,DrawShape,ShapeRotate,ShapeMirrorX,ShapeMirrorY,true,-1);
           break;
          case MouseOp.DrawRect:
           if(mopt!=null) {
             int x1=mopt[0],y1=mopt[1],x2=cx,y2=cy;
             if(DrawOrto) RectOrto(px,py,ref x1,ref y1,ref x2,ref y2);           
             RepeatDrawPara(pmcx,pmcy,x1,y1,x2,y2,0!=(pmk&3)?bmap.White:0,DrawBrush,DrawShape,ShapeRotate,ShapeMirrorX,ShapeMirrorY,ShapeAdjust,true,-1);
           } else RepeatDrawRect(px,py,cx,cy,0!=(pmk&3)?bmap.White:0,DrawBrush,DrawShape,ShapeRotate,ShapeMirrorX,ShapeMirrorY,ShapeAdjust,true,-1);
           break;
          case MouseOp.DrawEdge:
           RepeatDrawEdge(px,py,cx,cy,0!=(pmk&3)?bmap.White:0,DrawBrush,DrawShape,edge,edge2,ShapeRotate,ShapeMirrorX,ShapeMirrorY,true,-1);
           break;
          case MouseOp.Select:
           XorCross(pmcx,pmcy,cx,cy);
           if(movesel) {
             int dx=cx-pmcx,dy=cy-pmcy;
             DrawRect(Selection[0]+dx,Selection[1]+dy,Selection[2]+dx,Selection[3]+dy,bmap.White,DrawBrush,null,0,ShapeMirrorX,ShapeMirrorY,ShapeAdjust,true,-1);
           } else {
             DrawRect(pmcx,pmcy,cx,cy,0!=(pmk&3)?bmap.White:0,DrawBrush,null,0,ShapeMirrorX,ShapeMirrorY,ShapeAdjust,true,-1);
           }
           break;
          case MouseOp.DrawMorph:
           if(Morphs>1) DrawLine(Morph[0],Morph[1],Morph[2],Morph[3],0,DrawBrush,true,true);
           if(Morphs>2) DrawLine(Morph[2],Morph[3],Morph[4],Morph[5],0,DrawBrush,true,true);
           if(Morphs>3) DrawLine(Morph[4],Morph[5],Morph[6],Morph[7],0,DrawBrush,true,true);
           if(Morphs>=3) DrawLine(Morphs==3?cx:Morph[6],Morphs==3?cy:Morph[7],Morph[0],Morph[1],0,DrawBrush,true,true);
           if(Morphs>5) DrawLine(Morph[8],Morph[9],Morph[10],Morph[11],0,DrawBrush,true,true);
           if(Morphs>6) DrawLine(Morph[10],Morph[11],Morph[12],Morph[13],0,DrawBrush,true,true);
           if(Morphs>7) DrawLine(Morph[12],Morph[13],Morph[14],Morph[15],0,DrawBrush,true,true);
           if(Morphs>=7) DrawLine(Morphs==7?cx:Morph[14],Morphs==7?cy:Morph[15],Morph[8],Morph[9],0,DrawBrush,true,true);
           if(Morphs>0&&Morphs!=4&&Morphs<8) DrawLine(Morph[2*Morphs-2],Morph[2*Morphs-1],cx,cy,0,DrawBrush,true,true);
           break;
          case MouseOp.None:
           if(IsSelectionMode())
             DrawSelection();
           break;  
         }
        }
        
        private void fMain_MouseMove(object sender, MouseEventArgs e) {
          int cx=IX(e.X,e.Y),cy=IY(e.X,e.Y);
          if(mop!=MouseOp.Pan)
            MoveOp(cx,cy);
          else if(pmk==0) {
            bool ctrl=GDI.CtrlKey,shift=GDI.ShiftKey,alt=GDI.AltKey;
            if(ctrl||shift) {
              int z=zoom>ZoomBase?zoom:ZoomBase;
              if(ctrl) z=-z;
              sx+=(e.X-lmx)*z/ZoomBase;sy+=(e.Y-lmy)*z/ZoomBase;
            } else {
              sx+=e.X-lmx;sy+=e.Y-lmy;
            }
            timeDraw=true;
          }
          DrawXOR(); 
          lmx=e.X;lmy=e.Y;
          DrawXOR(); 
          if(IsStatusBar()) UpdateStatusBar();
        }
        void MoveOp(int cx,int cy) {
          switch(mop) {
           case MouseOp.DrawFree:{
            int nx=cx,ny=cy;
            if(AbsMaxDist(nx-freex,ny-freey)>=snap) { 
              Snap(ref nx,ref ny);
              RepeatDrawLine(nx,ny,freex,freey,DrawColor(pmk),DrawBrush,true,false,0);
              freex=nx;freey=ny;              
            }
            } break;
          }
           /*case MouseOp.DrawLine:
            DrawLine(pmcx,pmcy,IX(lmx,lmy),IY(lmx,lmy),0!=(pmk&3)?bmap.White:0,DrawBrush,true,true);
            DrawLine(pmcx,pmcy,cx,cy,0!=(pmk&3)?bmap.White:0,DrawBrush,true,true);
            break;
           case MouseOp.DrawPolar:
            DrawPolar(pmcx,pmcy,IX(lmx,lmy),IY(lmx,lmy),0!=(pmk&3)?bmap.White:0,DrawBrush,DrawShape,0,ShapeMirrorX,ShapeMirrorY,true);
            DrawPolar(pmcx,pmcy,cx,cy,0!=(pmk&3)?bmap.White:0,DrawBrush,DrawShape,0,ShapeMirrorX,ShapeMirrorY,true);
            break;
           case MouseOp.DrawRect:
            DrawRect(pmcx,pmcy,IX(lmx,lmy),IY(lmx,lmy),0!=(pmk&3)?bmap.White:0,DrawBrush,DrawShape,0,ShapeMirrorX,ShapeMirrorY,true);
            DrawRect(pmcx,pmcy,cx,cy,0!=(pmk&3)?bmap.White:0,DrawBrush,DrawShape,0,ShapeMirrorX,ShapeMirrorY,true);
            break;
           case MouseOp.DrawEdge:
            DrawEdge(pmcx,pmcy,IX(lmx,lmy),IY(lmx,lmy),0!=(pmk&3)?bmap.White:0,DrawBrush,DrawShape,0,ShapeMirrorX,ShapeMirrorY,true);
            DrawEdge(pmcx,pmcy,cx,cy,0!=(pmk&3)?bmap.White:0,DrawBrush,DrawShape,0,ShapeMirrorX,ShapeMirrorY,true);
            break;
           case MouseOp.Select:
            if(movesel) {
              int dx=IX(lmx,lmy)-pmcx,dy=IY(lmx,lmy)-pmcy;
              DrawRect(Selection[0]+dx,Selection[1]+dy,Selection[2]+dx,Selection[3]+dy,bmap.White,DrawBrush,null,0,ShapeMirrorX,ShapeMirrorY,true);
              dx=cx-pmcx;dy=cy-pmcy;  
              DrawRect(Selection[0]+dx,Selection[1]+dy,Selection[2]+dx,Selection[3]+dy,bmap.White,DrawBrush,null,0,ShapeMirrorX,ShapeMirrorY,true);
            } else {
              DrawRect(pmcx,pmcy,IX(lmx,lmy),IY(lmx,lmy),0!=(pmk&3)?bmap.White:0,DrawBrush,null,0,ShapeMirrorX,ShapeMirrorY,true);
              DrawRect(pmcx,pmcy,cx,cy,0!=(pmk&3)?bmap.White:0,DrawBrush,null,0,ShapeMirrorX,ShapeMirrorY,true);
            }
            break;*/
        }
        int DrawColor(int pmk) {
          bool shift=0!=(pmk&1),alt=0!=(pmk&4),ctrl=0!=(pmk&2);
          return alt||ctrl&&shift?bmap.White:ctrl?(DrawBlack?Color1:0):shift?Color2:(DrawBlack?0:Color1);
        }
        int FillColor(int pmk) {
          bool shift=0!=(pmk&1),alt=0!=(pmk&4),ctrl=0!=(pmk&2);
          return alt||ctrl&&shift?bmap.Black:ctrl?Color1:shift?bmap.White:Color2;
        }
        void RectOrto(int x0,int y0,int x1,int y1,ref int x2,ref int y2) {          
          int dx=y0-y1,dy=x0-x1,ex=x2-x1,ey=y2-y1;
          if((dx!=0||dy!=0)&&(ex!=0||ey!=0)) {
            int dd=dx*dx+dy*dy;
            int k=ex*dx-ey*dy;
            x2=x1+k*dx/dd;y2=y1-k*dy/dd;
          }
        }
        void RectOrto(int x0,int y0,ref int x1,ref int y1,ref int x2,ref int y2) {          
          int dx=x1-x0,dy=y1-y0,ex=x2-x0,ey=y2-y0;
          if(dx==0&&dy==0) return;
          int dd=dx*dx+dy*dy;
          int k=ex*dx+ey*dy;
          x1=x0+k*dx/dd;y1=y0+k*dy/dd;
          if(ex!=0||ey!=0) {            
            k=-ex*dy+ey*dx;
            x2=x1-k*dy/dd;y2=y1+k*dx/dd;
          }
        }
				void Erase(int px,int py,int cx,int cy,bool dox,bool doy) {
				  MopUndo();
					map.Erase(px+1,py+1,cx+1,cy+1,dox,doy);
					Repaint(px,py,cx,cy,true);
					if(mop==MouseOp.Select) DrawXOR();
				}
        void ReplaceStrip(int cx,int cy,bool vert,bool undo) {
          int x0,y0,x1,y1;          
          if(IsSelectionEmpty()) {x0=y0=1;x1=bm.Width;y1=bm.Height;}
          else { 
            x0=Selection[0]+1;y0=Selection[1]+1;x1=Selection[2]+1;y1=Selection[3]+1;
            if(x0==x1) {x0=1;x1=bm.Width;}
            else if(y0==y1) { y0=1;y1=bm.Height;vert=true;}
          }
          if(undo) PushUndo();
          map.ReplaceStrip(vert,vert?cy+1:cx+1,bmap.White,x0,y0,x1,y1);
          Repaint(x0,y0,x1,y1,true);
          
        }
        void DeleteInMop(MouseOp pmop,int px,int py,int cx,int cy,int keys) {
          int color=FillColor(keys);
          if(IsSnap(pmop)) {Snap(ref px,ref py);Snap(ref cx,ref cy);}
          switch(pmop) {
           case MouseOp.FillShape:
           case MouseOp.FillLinear:
           case MouseOp.FillRadial:case MouseOp.FillSquare:
           case MouseOp.Fill4:case MouseOp.FillVect:
           case MouseOp.FillFlood:case MouseOp.FillFloat:
           case MouseOp.FillBorder:
            if(!mopundo) {PushUndo();mopundo=true;}            						
            map.FloodFill(new int[] {cx+1,cy+1},color,color,X8,GDI.CapsLock,0,Fill2Black,new PathPoint(0,0).List(),false,Pattern,Board.fillDown.Checked,Board.FillMix,Board.GetGammax());
            Repaint(true);
            break;           
           case MouseOp.DrawPolar:
            if(mopt!=null) {
              int x1=mopt[0],y1=mopt[1],x2=cx,y2=cy;
              if(DrawOrto) RectOrto(px,py,ref x1,ref y1,ref x2,ref y2);                       
              RepeatDrawPara(px-x2+x1,py-y2+y1,x1-x2+x1,y1-y2+y1,x2,y2,-1,DrawBrush,DrawShape,ShapeRotate,ShapeMirrorX,ShapeMirrorY,ShapeAdjust,false,color);
            } else RepeatDrawPolar(px,py,cx,cy,-1,DrawBrush,DrawShape,ShapeRotate,ShapeMirrorX,ShapeMirrorY,false,color);
            break;
           case MouseOp.DrawRect:
             if(mopt!=null) {
               int x1=mopt[0],y1=mopt[1],x2=cx,y2=cy;
               if(DrawOrto) RectOrto(px,py,ref x1,ref y1,ref x2,ref y2);
               RepeatDrawPara(px,py,x1,y1,x2,y2,-1,DrawBrush,DrawShape,ShapeRotate,ShapeMirrorX,ShapeMirrorY,ShapeAdjust,false,color);
             } else RepeatDrawRect(px,py,cx,cy,-1,DrawBrush,DrawShape,ShapeRotate,ShapeMirrorX,ShapeMirrorY,ShapeAdjust,false,color);
            break;
           case MouseOp.DrawEdge:
            RepeatDrawEdge(px,py,cx,cy,-1,DrawBrush,DrawShape,edge,edge2,ShapeRotate,ShapeMirrorX,ShapeMirrorY,false,color);
            break;
           case MouseOp.Select:
            if(!mopundo) {PushUndo();mopundo=true;}
						int cx2=cx,cy2=cy;
						R.Norm(ref px,ref py,ref cx,ref cy);
            if(0!=(keys&3)) map.Erase(px+1,py+1,cx+1,cy+1,1!=(keys&3),3!=(keys&3));
            else {
              int c=map.XY(cx2+1,cy2+1);
              if(c>=0) 
                map.FillRectangle(px+1,py+1,cx+1,cy+1,c);
            }
            Repaint(px,py,cx,cy,true);
            break;
          }
        }
        void FinishMop(MouseOp pmop,int px,int py,int cx,int cy) {         
         if(IsSnap(pmop)) {Snap(ref px,ref py);Snap(ref cx,ref cy);}
         if(DrawCenter&&(pmop==MouseOp.DrawRect||pmop==MouseOp.DrawPolar||pmop==MouseOp.DrawEdge)) {px+=px-cx;py+=py-cy;}
         int fill;
         switch(pmop) {
          case MouseOp.DrawLine:
           RepeatDrawLine(px,py,cx,cy,DrawColor(pmk),DrawBrush,true,false,(GDI.ShiftKey?1:0)|(GDI.CtrlKey?2:0)|(GDI.AltKey?4:0));
           break;
          case MouseOp.DrawPolar:
           fill=GDI.CtrlKey^DrawFilled?Color2:-1;
           if(mopt!=null) {
             int x1=mopt[0],y1=mopt[1],x2=cx,y2=cy;
             if(DrawOrto) RectOrto(px,py,ref x1,ref y1,ref x2,ref y2);             
             RepeatDrawPara(px-x2+x1,py-y2+y1,x1-x2+x1,y1-y2+y1,x2,y2,DrawColor(pmk),DrawBrush,DrawShape,ShapeRotate,ShapeMirrorX,ShapeMirrorY,ShapeAdjust,false,fill);
           } else RepeatDrawPolar(px,py,cx,cy,DrawColor(pmk),DrawBrush,DrawShape,ShapeRotate,ShapeMirrorX,ShapeMirrorY,false,fill);
           break;
          case MouseOp.DrawRect:
          fill=GDI.CtrlKey^DrawFilled?Color2:-1;
           if(mopt!=null) {
             int x1=mopt[0],y1=mopt[1],x2=cx,y2=cy;
             if(DrawOrto) RectOrto(px,py,ref x1,ref y1,ref x2,ref y2);
             RepeatDrawPara(px,py,x1,y1,x2,y2,DrawColor(pmk),DrawBrush,DrawShape,ShapeRotate,ShapeMirrorX,ShapeMirrorY,ShapeAdjust,false,fill);
           } else RepeatDrawRect(px,py,cx,cy,DrawColor(pmk),DrawBrush,DrawShape,ShapeRotate,ShapeMirrorX,ShapeMirrorY,ShapeAdjust,false,fill);
           break;
          case MouseOp.DrawEdge:
           fill=GDI.CtrlKey^DrawFilled?Color2:-1;
           RepeatDrawEdge(px,py,cx,cy,DrawColor(pmk),DrawBrush,DrawShape,edge,edge2,ShapeRotate,ShapeMirrorX,ShapeMirrorY,false,fill);
           break;
          case MouseOp.DrawMorph:
           if(Morphs<8) {
             Morph[2*Morphs]=cx;
             Morph[2*Morphs+1]=cy;
             Morphs++;
           } 
           if(Morphs==7) {
             Morph[14]=Morph[12]+Morph[8]-Morph[10];
             Morph[15]=Morph[13]+Morph[9]-Morph[11];
             Morphs=8;
           }
           if(Morphs==8)
             DrawMorph();
           Repaint(true,false);
           Morphs=4;
           break;
          case MouseOp.FillShape:
          case MouseOp.FillLinear:
          case MouseOp.FillRadial:case MouseOp.FillSquare:
          case MouseOp.Fill4:case MouseOp.FillVect:
          case MouseOp.FillFlood:case MouseOp.FillFloat:
          case MouseOp.FillBorder:
           if(!mopundo) {PushUndo();mopundo=true;}
           if(mop==MouseOp.FillFlood||mop==MouseOp.FillBorder||mop==MouseOp.Fill4||mop==MouseOp.FillVect||mop==MouseOp.FillSquare) {
             Array.Resize(ref mopt,mopt==null?4:mopt.Length+4);
             int i=mopt.Length-4;
             mopt[i]=cx+1;mopt[i+1]=cy+1;mopt[i+2]=px+1;mopt[i+3]=py+1;
             switch(mop) {
              //case MouseOp.FillBorder:map.FloodFillGrad(mopt,Color1,Board.chFillMono.Checked?Color1:Color2,GDI.CtrlKey?2:1,FillD8,X8,true,Fill2Black,Board.fillDown.Checked,Board.FillLimit,Fill2Black?undomap:null);break;
              case MouseOp.FillBorder:map.FloodBorder(mopt,Color1,Board.chFillMono.Checked?Color1:Color2,true,Fill2Black,X8,FillD8,Board.GetGammax());break;
              case MouseOp.Fill4:map.FloodFill4(mopt,Color1,Board.chFillMono.Checked?Color1:Color2,true,Fill2Black,X8,Board.Fill4(),Board.GetGammax());break;
              case MouseOp.FillSquare:map.FloodSquare(px+1,py+1,cx+1,cy+1,GDI.CapsLock,Color1,Color2,true,Fill2Black,X8,Board.FillSquare(),Board.GetGammax());break;
              case MouseOp.FillVect:{
                int dx=py-cy,dy=-(px-cx),d=dx*dx+dy*dy;
                if(d<1) dx=1;else if(d>16384) { d=bmap.isqrt(d);dx=dx*128/d;dy=dy*128/d;}
                map.FloodVector(mopt,Color1,Board.chFillMono.Checked?Color1:Color2,true,Fill2Black,X8,dx,dy,true,Board.GetGammax());
               } break;
              default:map.FloodFillGrad(mopt,Color1,Board.chFillMono.Checked?Color1:Color2,0,FillD8,X8,true,Fill2Black,Board.fillDown.Checked,Board.FillLimit,null,Board.GetGammax());break;
             }
             Repaint(true);
           } else {
             bool ctrl=GDI.CtrlKey;
             Array.Resize(ref mopt,mopt==null?2:mopt.Length+2);
             int i=mopt.Length-2;
             mopt[i]=px+1;mopt[i+1]=py+1;
						 gxy.Add(new PathPoint(cx+1,cy+1));
             fillres fr;
						 bool noblack=!GDI.ShiftKey;
             if(mop==MouseOp.FillFloat)
                fr=map.FloodFloat(mopt,Color1,Board.chFillMono.Checked?Color1:Color2,true,Fill2Black,X8,gxy,Board.GetGammax());
             else if(mop==MouseOp.FillLinear||mop==MouseOp.FillRadial)
               fr=map.FloodFill(mopt,ctrl?bmap.White:Color1,ctrl?bmap.White:Board.chFillMono.Checked?Color1:Color2,X8,noblack,mop==MouseOp.FillRadial?-2:-1,noblack&&Fill2Black,gxy,false,Pattern,Board.fillDown.Checked,Board.FillMix,Board.GetGammax());
             else
						   fr=map.FloodFill(mopt,ctrl?bmap.White:Color1,ctrl?bmap.White:Board.chFillMono.Checked?Color1:Color2,X8,noblack,GradMode,noblack&&Fill2Black,gxy,false,Pattern,Board.fillDown.Checked,Board.FillMix,Board.GetGammax());
						 gxy.Clear();
             if(fr.m>0) Repaint(fr.x0-1,fr.y0-1,fr.x1-1,fr.y1-1,true);
           }
           break;
          case MouseOp.Replace:{
            if(!mopundo) {PushUndo();mopundo=true;}
            bool ctrl=GDI.CtrlKey,shift=GDI.ShiftKey,alt=GDI.AltKey,ns=IsSelectionEmpty();
            DrawSelection();
            int cmode=Board.GetReplace('R');
            if(cmode>0) {
              int x0=ns?1:Selection[0]+1,y0=ns?1:Selection[1]+1,x1=ns?map.Width-2:Selection[2]+1,y1=ns?map.Height-2:Selection[3]+1;
              map.Colorize(cmode,Color1,Board.chFillMono.Checked?Color1:Color2,GradMode,cx+1,cy+1,false,x0,y0,x1,y1,Board.ReplaceNoBorder.Checked);
              Repaint(x0-1,y0-1,x1-1,y1-1,true);            
            } else {
              fillres fr=map.Replace(px+1,py+1,Color1,Board.chFillMono.Checked?Color1:Color2,false,GradMode,cx+1,cy+1,false,ns?1:Selection[0]+1,ns?1:Selection[1]+1,ns?map.Width-2:Selection[2]+1,ns?map.Height-2:Selection[3]+1,Board.ReplaceNoBorder.Checked);
              if(fr.m>0) Repaint(fr.x0-1,fr.y0-1,fr.x1-1,fr.y1-1,true);
            }
            DrawSelection();
           } break;
          case MouseOp.Select:
            XorCross(pmcx,pmcy,cx,cy);
            if(movesel) {
              int dx=IX(lmx,lmy)-pmcx,dy=IY(lmx,lmy)-pmcy;
              DrawRect(Selection[0]+dx,Selection[1]+dy,Selection[2]+dx,Selection[3]+dy,bmap.White,DrawBrush,null,0,ShapeMirrorX,ShapeMirrorY,ShapeAdjust,true,-1);
              //DrawSelection();
              dx=cx-pmcx;dy=cy-pmcy;
              bool move=0!=(pmk&4),copy=0!=(pmk&2);
              if(!mopundo&&(copy||!MovePaste)) {
                PushUndo();                
              }
              if(copy||move) MovePaste=true;
              MoveSelection(true,dx,dy,0!=(pmk&2),0!=(pmk&1));
            } else {
              Selection[0]=pmcx;Selection[1]=pmcy;Selection[2]=cx;Selection[3]=cy;
              NormSelection();
            }
            UndoCode();
            break;
         }
        }
        private void fMain_MouseUp(object sender,MouseEventArgs e) {
          if(e.Button!=pmb) return;
          int cx=IX(e.X,e.Y),cy=IY(e.X,e.Y);
          switch(mop) {
           case MouseOp.FillShape:
           case MouseOp.FillLinear:
           case MouseOp.FillRadial:case MouseOp.FillSquare:
           case MouseOp.Fill4:case MouseOp.FillVect:
           case MouseOp.FillFlood:case MouseOp.FillFloat:
           case MouseOp.FillBorder:
           case MouseOp.Replace:
             FinishMop(mop,pmcx,pmcy,cx,cy);
             break;           
/*            if(!mopundo) {PushUndo();mopundo=true;}
            if(mop==MouseOp.FillFlood||mop==MouseOp.FillBorder) {
              Array.Resize(ref mopt,mopt==null?4:mopt.Length+4);
              int i=mopt.Length-4;
              mopt[i]=cx+1;mopt[i+1]=cy+1;mopt[i+2]=pmcx+1;mopt[i+3]=pmcy+1;
              switch(mop) {
               case MouseOp.FillBorder:map.FloodFillGrad(mopt,Color1,Color2,true,GradD8,GDI.CapsLock,true,Fill2Black);break;
               default:map.FloodFillGrad(mopt,Color1,Color2,false,GradD8,GDI.CapsLock,true,Fill2Black);break;
              }
              Repaint(true);
            } else {
              bool ctrl=GDI.CtrlKey;
              Array.Resize(ref mopt,mopt==null?2:mopt.Length+2);
              int i=mopt.Length-2;
              mopt[i]=pmcx+1;mopt[i+1]=pmcy+1;
              fillres fr=map.FloodFill(mopt,ctrl?bmap.White:Color1,ctrl?bmap.White:Color2,GDI.CapsLock,!GDI.ShiftKey,GradMode,Fill2Black,cx+1,cy+1,false);              
              if(fr.m>0) Repaint(fr.x0-1,fr.y0-1,fr.x1-1,fr.y1-1,true);
            }
            break;*/
           case MouseOp.Pan:
            if(0!=(3&pmk)) {
              ZoomTo(pmcx,pmcy,cx,cy,0!=(pmk&1),3==(pmk&3));
              timeDraw=true;
            }            
            break;
           case MouseOp.DrawLine:
           case MouseOp.DrawPolar:
           case MouseOp.DrawRect:
           case MouseOp.DrawEdge:
           case MouseOp.DrawMorph:
           case MouseOp.Select:
            FinishMop(mop,pmcx,pmcy,cx,cy);
            break;
          }             
          lmx=e.X;lmy=e.Y;pmb=MouseButtons.None;mop=MouseOp.None;mopt=null;mopundo=false;
        } 
        void CancelMouse() {
          if(mop==MouseOp.None&&pmb==MouseButtons.None) return; 
          mop=MouseOp.None;
          pmb=MouseButtons.None;
          mopt=null;
          Repaint(false);          
        }
        void ZoomTo(int x0,int y0,int x1,int y1,bool bigger,bool z100) {
          Rectangle cr=ClientRectangle;
          int r;
          if(x1<x0) {r=x1;x1=x0;x0=r;}
          if(y1<y0) {r=y1;y1=y0;y0=r;}
          x1-=x0;y1-=y0;
          if(x1==0&&y1==0) {
            x0=y0=0;x1=map.Width-2;y1=map.Height-1;
          }
          x1++;y1++;
          int zx=cr.Width*ZoomBase/x1,zy=cr.Height*ZoomBase/y1;
          if(zy<zx^bigger) zx=zy;
          if(zx>ZoomBase*16) zx=ZoomBase*16;
          else if(zx<12) zx=12;
          zoom=z100?ZoomBase:zx;
          sx=cr.Width/2-(x0+x1/2)*zoom/ZoomBase;
          sy=cr.Height/2-(y0+y1/2)*zoom/ZoomBase;
        }       


        private void miFileSave_Click(object sender, EventArgs e) {
          ToolStripItem c=sender as ToolStripDropDownItem;
          string s=""+c.Name;
          SaveFile(GDI.ShiftKey,s.EndsWith("SaveIcon"));
        }
        private void miFileSaveas_Click(object sender, EventArgs e) {
          SaveFile(true,false); 
        }

#if GRC        
        static bool IsGRC(string filename) {
          return filename.EndsWith(".grc",StringComparison.InvariantCultureIgnoreCase);
				}
				const int GRCSig=0x00435247;
#endif
				bool SaveFile(bool saveas,bool icon) {
          if(string.IsNullOrEmpty(ofd.FileName)||saveas) {
            if(sfd==null) sfd=new SaveFileDialog();
            sfd.FileName=ofd.FileName;
            sfd.Filter=FileFilter;
						sfd.FilterIndex=ofd.FilterIndex;
            sfd.Title=saveas?"Save as":"Save";
            if(DialogResult.OK!=sfd.ShowDialog()) return false;
						ofd.FilterIndex=sfd.FilterIndex;
            string fname=sfd.FileName;
            if(Path.GetExtension(fname)=="") fname+=".png";
            ChangeFileName(fname);
          }
				 try {
          if(icon) {
            int iw,ih;
            if(!Board.GetIconSize(bm.Width,bm.Height,out iw,out ih)) return false;
            bool cursor=Board.chIconCursor.Checked;
            Bitmap tr=GetIcon(iw,ih,false,Board.GetIconTrColor(false));//GetBitmap(bmap.White);
            string fname=Path.GetFileNameWithoutExtension(ofd.FileName);
            int cx=0,cy=0;
            if(cursor&&!IsSelectionEmpty()) {cx=Selection[0]*iw/bm.Width;cy=Selection[1]*ih/bm.Height;}
						SaveIcon(tr,fname+(cursor?".cur":".ico"),cursor,cx,cy);
            //tr.Save(fname+"_icon.png");
            tr.Dispose();
						return true;
          } 
#if GRC
					else if(IsGRC(ofd.FileName)) SaveGRC(bm,ofd.FileName);
#endif
					else {
            string ext=Path.GetExtension(ofd.FileName);
            ImageFormat fmt=ImageFormat.Png;
            switch(ext) {
             case ".jpg":
             case ".jpeg":fmt=ImageFormat.Jpeg;break;
             case ".bmp":fmt=ImageFormat.Bmp;break;
             case ".gif":fmt=ImageFormat.Gif;break;
             case ".tiff":fmt=ImageFormat.Tiff;break;
            }
            Bitmap bs=bm;
            if(Board.chSaveTrans.Checked) {
              bs=bm.Clone() as Bitmap;              
              bs.MakeTransparent(IntColor(Board.GetIconTrColor(true)));
            } 
            bs.Save(ofd.FileName,fmt);
          }
          UnsetDirty();
          return true;
				 } catch(Exception ex) {
				  MessageBox.Show(this,ex.Message,"Save",MessageBoxButtons.OK,MessageBoxIcon.Error);
				  return false;
				 }
        }
				static void SaveIcon(Bitmap bm,string filename,bool cursor,int cx,int cy) {
				  int Width=bm.Width,Height=bm.Height;
					if(Width>256||Height>256) return;
					int bpl=Width*4,mbpl=((Width+31)&~31)/8;
          //bool cur=filename.EndsWith(".cur",StringComparison.InvariantCulture);
          BinaryWriter bw=new BinaryWriter(new FileStream(filename,FileMode.Create,FileAccess.Write));
          bw.Write((ushort)0);bw.Write((ushort)(cursor?2:1));bw.Write((ushort)1);

					bw.Write((byte)Width);bw.Write((byte)Height);
					bw.Write((byte)0);bw.Write((byte)0);
          if(cursor) {
            if(cx<0) cx=0;else if(cx>=Width) cx=Width-1;
            if(cy<0) cy=0;else if(cy>=Height) cy=Height-1;
            bw.Write((ushort)cx);bw.Write((ushort)cy); 
          } else {
					  bw.Write((ushort)1);bw.Write((ushort)32);
          }
					bw.Write(40+(bpl+mbpl)*Height);
					bw.Write(6+16);

					bw.Write(40);
					bw.Write(Width);bw.Write(Height*2);
					bw.Write((ushort)1);bw.Write((ushort)32);
					bw.Write(0);bw.Write(bpl*Height);
					bw.Write(0);bw.Write(0);bw.Write(0);bw.Write(0);

					BitmapData bd=bm.LockBits(new Rectangle(0,0,Width,Height),ImageLockMode.ReadOnly,PixelFormat.Format32bppArgb);
					byte[] Data=new byte[4*Width];
					byte[] Mask=new byte[mbpl*Height];
					for(int y=0,y2=Height-1;y<Height;y++,y2--) {
					  Marshal.Copy(new IntPtr(bd.Scan0.ToInt64()+y2*bd.Stride),Data,0,4*Width);
						bw.Write(Data,0,Data.Length);
						int mp=y*mbpl;
						for(int x=0;x<Width;x++) {
						  bool off=Data[4*x+3]!=0;
							if(off) Mask[mp+x/8]|=(byte)(1<<(7-(x&7)));
						}
					}
					bm.UnlockBits(bd);
					bw.Write(Mask,0,Mask.Length);
					bw.Close();
				}

#if GRC
				static Bitmap LoadGRC(string filename) {
					Bitmap bm=null;
				  using(BinaryReader br=new BinaryReader(new FileStream(filename,FileMode.Open,FileAccess.Read))) {
					int sig=br.ReadInt32();
					if(sig!=GRCSig) throw new Exception("not grc file");
					int w,h;          
					w=br.ReadInt32();h=br.ReadInt32();
					bm=new Bitmap(w,h,PixelFormat.Format32bppRgb);
					BitmapData bd=bm.LockBits(new Rectangle(0,0,w,h),ImageLockMode.ReadOnly,PixelFormat.Format32bppRgb);
					GZipStream gz=new GZipStream(br.BaseStream,CompressionMode.Decompress);
					byte[] Data=new byte[4*w];
					byte[] data=new byte[w];
					for(int y=0;y<h;y++) {
						for(int j=0;j<3;j++) {
						  int r=gz.Read(data,0,data.Length);
							byte b2=0,b;
							for(int i=0;i<w;i++) {
							  b=(byte)(data[i]-128+b2);
								if(j>0) b+=Data[4*i];
								Data[4*i+j]=b;
								b2=Data[4*i+j];
								if(j>0) b2-=Data[4*i];
							}
						}
						Marshal.Copy(Data,0,new IntPtr(bd.Scan0.ToInt64()+y*bd.Stride),4*w);
					}
					bm.UnlockBits(bd);
				 }
					return bm;
				}
				static void SaveGRC(Bitmap bm,string filename) {
				int Width=bm.Width,Height=bm.Height;
				BitmapData bd=bm.LockBits(new Rectangle(0,0,Width,Height),ImageLockMode.ReadOnly,PixelFormat.Format32bppRgb);
        BinaryWriter bw=new BinaryWriter(new FileStream(filename,FileMode.OpenOrCreate,FileAccess.Write));
        bw.Write(GRCSig);
				bw.Write(Width);
        bw.Write(Height);
        bw.Flush();
        GZipStream gz=new GZipStream(bw.BaseStream,CompressionMode.Compress);
				//GZipStream gz=new GZipStream(bw.BaseStream,CompressionLevel.Optimal); //4.5.1
				byte[] Data=new byte[4*Width];
        byte[] data=new byte[Width];				
				for(int y=0;y<Height;y++) {
				  Marshal.Copy(new IntPtr(bd.Scan0.ToInt64()+y*bd.Stride),Data,0,4*Width);
          for(int j=0;j<3;j++) {
            byte b2=0,b=0;
            int i=0;
            for(int d=0;d<data.Length;i+=4) {
						  b=Data[i];
							if(j>0) b=(byte)(Data[i+j]-b);
              //data[d++]=Data[i+j];
              data[d++]=(byte)(b-b2+128);
              b2=b;
            }
            gz.Write(data,0,data.Length);
          }
        }
        gz.Flush();
        gz.BaseStream.SetLength(gz.BaseStream.Position);
        gz.Close();				  

				bm.UnlockBits(bd);
				}
#endif

        bool CheckDirty(string caption) {
          if (!Dirty) return true;
          DialogResult dr=MessageBox.Show(this,"Save changes?",caption,MessageBoxButtons.YesNoCancel,MessageBoxIcon.Exclamation,MessageBoxDefaultButton.Button3);
          if(dr!=DialogResult.Yes) return dr==DialogResult.No;
          return SaveFile(false,false);
        }
        
        void SetDirty() {
          if(Dirty) return;
          Dirty=true;
          if(!Text.EndsWith("*")) Text+="*";
        }
        void UnsetDirty() {
          if(!Dirty) return;
          Dirty=false;
          if(Text.EndsWith("*")) Text=Text.Substring(0,Text.Length-1);        
        }

        void NewFile() {
          if(!CheckDirty("New file")) return;          
          ChangeFileName(null);
          NewMap(true);
        }
        private void miFileOpen_Click(object sender, EventArgs e) {
          OpenFile(GDI.CtrlKey|GDI.ShiftKey,GDI.CtrlKey?GDI.CtrlRKey?5:1:GDI.ShiftKey?GDI.ShiftRKey?6:2:0);
        }
        static bool IsMatch(string text,string pattern) {
          return Regex.IsMatch(text,pattern,RegexOptions.IgnoreCase|RegexOptions.ExplicitCapture);
        }
				const string FileFilter="*.png|*.png|*.jpg|*.jpg;*.jpeg|*.bmp|*.bmp|*.gif|*.gif"
#if GRC
				  +"|*.grc|*.grc"
#endif
				  +"|bitmap|*.png;*.bmp;*.gif;*.jpg;*.jpeg|*.*|*.*";       
        void OpenFile(bool merge,int append) {
          if(!merge&&!CheckDirty("Open file")) return;
          string dir=Directory.GetCurrentDirectory();
          ofd.Title="Open";
          ofd.Filter=FileFilter;                        
          ofd.CheckFileExists=true;
          string fn=ofd.FileName;
          if(ofd.FilterIndex!=5&&ofd.FilterIndex!=6) 
            ofd.FilterIndex=IsMatch(fn,@".png")?1:IsMatch(fn,@"\.jpe?g$")?2:IsMatch(fn,@"\.bmp")?3:IsMatch(fn,@"\.gif$")?4:0;
          ofd.DefaultExt="png";
          if(DialogResult.OK==ofd.ShowDialog(this)) {
            if(merge||append>0) {
              string of=ofd.FileName;
              ofd.FileName=fn;
              Bitmap ff=Bitmap.FromFile(of) as Bitmap;
              PasteSelection(ff,Board.pasteTrans.Checked?Color2:-1,Board.pasteTRX.Checked,Board.GetDiff(),Board.pasteRepeat.Checked,Board.pasteQuad.Checked,Board.pasteExtend.Checked,Board.GetMix(),Board.GetPasteFilter(),append);
            } else {
              ClearUndo();
              LoadFile(ofd.FileName,true,0,false);
            }
          }
          Directory.SetCurrentDirectory(dir);
        }
        void Reload() {          
          if(""+ofd.FileName!="") LoadFile(ofd.FileName,true,0,true);
        }

        protected override void OnClosing(CancelEventArgs e) {
          if(!CheckDirty("Close window")) e.Cancel=true;
        }

        private void miFilePage_Click(object sender, EventArgs e) { PrintPage();}
        private void miFilePrint_Click(object sender, EventArgs e) { Print();}
        void PrintPage() {
          if(paged==null) {
            paged=new PageSetupDialog();
            paged.EnableMetric=true;
            paged.AllowPaper=paged.AllowMargins=paged.AllowOrientation=true;            
            paged.PageSettings=new System.Drawing.Printing.PageSettings() {Landscape=Board.rLandscape.Checked};
            paged.PageSettings.Margins=new System.Drawing.Printing.Margins(0,0,0,0);
          }
          paged.ShowDialog();
          Board.rLandscape.Checked=paged.PageSettings.Landscape;
        }
        int PrintMulti,PrintNumber;
        void Print() {
          if(map==null) return;          
          if(paged==null) PrintPage();          
          if(printd==null) {
            printd=new PrintDialog();
						printd.UseEXDialog=true;
          }
          int multi=Board.GetPrintMulti();
          PrintMulti=multi;
          PrintNumber=0;
          printd.PrinterSettings.DefaultPageSettings.Landscape=paged.PageSettings.Landscape=Board.rLandscape.Checked;
          if(DialogResult.OK==printd.ShowDialog(this)) {
            using(System.Drawing.Printing.PrintDocument doc=new System.Drawing.Printing.PrintDocument()) {
              paged.PageSettings.Landscape=printd.PrinterSettings.DefaultPageSettings.Landscape;
              doc.DocumentName=Text+(multi==2?" 2x1":multi==3?" 3x1":multi==4?" 2x2":multi==6?" 3x2":"");
              doc.PrinterSettings=printd.PrinterSettings;
              doc.DefaultPageSettings=paged.PageSettings.Clone() as System.Drawing.Printing.PageSettings;
              doc.PrintPage+=new System.Drawing.Printing.PrintPageEventHandler(doc_PrintPage);
              doc.Print();
            }
          }
        }
        
        void doc_PrintPage(object sender,System.Drawing.Printing.PrintPageEventArgs e) {
          Graphics gr=e.Graphics;
          Rectangle rect=e.MarginBounds;
          PrintNumber++;
          Rectangle bmx;
          bool bl,br,bt,bb;
          if(PrintMulti==6) {
            int w2=bm.Width/2,w3=bm.Width/3,h2=bm.Height/2,h3=bm.Height/3;
            if(3*w3<bm.Width-1) w3++;if(3*h3<bm.Height-1) h3++;
            if((rect.Width*h2>rect.Height*w3?rect.Height*w3/h2:w3)<(rect.Width*h3>rect.Height*w2?rect.Height*w2/h3:w2)) {
              int dx=(PrintNumber-1)%3,dy=(PrintNumber-1)/3;
              bmx=new Rectangle(dx==1?w3:dx==2?2*w3:0,dy==1?h2:0,dx==2?bm.Width-2*w3:w3,dy==1?bm.Height-h2:h2);
            } else {
              int dx=(PrintNumber-1)/3,dy=(PrintNumber-1)%3;
              bmx=new Rectangle(dx==1?w2:0,dy==1?h3:dy==2?2*h3:0,dx==1?bm.Width-w2:w2,dy==2?bm.Height-2*h3:h3);
            }
            bl=bmx.Left>0;br=bmx.Left<w2;bt=bmx.Top>0;bb=bmx.Top<h2;
          } else if(PrintMulti==4) {
            int w2=bm.Width/2,h2=bm.Height/2;
            bmx=new Rectangle(
               PrintNumber==2||PrintNumber==4?w2:0
              ,PrintNumber==3||PrintNumber==4?h2:0
              ,PrintNumber==1||PrintNumber==3?w2:bm.Width-w2
              ,PrintNumber==1||PrintNumber==2?h2:bm.Height-h2
            );
            bl=bmx.Left>0;br=!bl;bt=bmx.Top>0;bb=!bt;
          } else if(PrintMulti==3) {
            int w3=bm.Width/3,h3=bm.Height/3,pn=PrintNumber-1;
            if((rect.Width*bm.Height>rect.Height*w3?rect.Height*w3/bm.Height:w3)<(rect.Width*h3>rect.Height*bm.Width?rect.Height*bm.Width/h3:bm.Width)) {
              bmx=new Rectangle(pn*w3,0,PrintNumber<3?w3:bm.Width-2*w3,bm.Height);
              bl=bmx.Left>0;br=PrintNumber<3;bb=bt=false;
            } else {
              bmx=new Rectangle(0,pn*h3,bm.Width,PrintNumber<3?h3:bm.Height-2*h3);
              bl=br=false;bt=bmx.Top>0;bb=PrintNumber<3;
            }
          } else if(PrintMulti==2) {
            int w2=bm.Width/2,h2=bm.Height/2;
            if((rect.Width*bm.Height>rect.Height*w2?rect.Height*w2/bm.Height:w2)<(rect.Width*h2>rect.Height*bm.Width?rect.Height*bm.Width/h2:bm.Width)) {
              bmx=new Rectangle(PrintNumber==1?0:w2,0,PrintNumber==1?w2:bm.Width-w2,bm.Height);
              bl=bmx.Left>0;br=!bl;bb=bt=false;
            } else {
              bmx=new Rectangle(0,PrintNumber==1?0:h2,bm.Width,PrintNumber==1?h2:bm.Height-h2);
              bl=br=false;bt=bmx.Top>0;bb=!bt;
            }
          } else {
            bmx=new Rectangle(0,0,bm.Width,bm.Height);          
            bl=br=bt=bb=false;
          }
          if(rect.Width*bmx.Height>rect.Height*bmx.Width) {
            int w=rect.Width-rect.Height*bmx.Width/bmx.Height;
            if(br&&!bl) rect.X+=w;
            else if(bl==br) rect.X+=w/2;
            rect.Width-=w;            
          } else {
            int h=rect.Height-rect.Width*bmx.Height/bmx.Width;
            if(bb&&!bt) rect.Y+=h;
            else if(bt==bb) rect.Y+=h/2;
            rect.Height-=h;
          }
          gr.DrawImage(bm,rect,bmx.Left,bmx.Top,bmx.Width,bmx.Height,GraphicsUnit.Pixel);
          if(bl||br||bt||bb) {
            Pen p=new Pen(Color.Black,0.25f);          
            if(bl) gr.DrawLine(p,rect.Left-1,rect.Top-(bt?1:0),rect.Left-1,rect.Bottom+(bb?1:0));
            if(br) gr.DrawLine(p,rect.Right+1,rect.Top-(bt?1:0),rect.Right+1,rect.Bottom+(bb?1:0));
            if(bt) gr.DrawLine(p,rect.Left-(bl?1:0),rect.Top-1,rect.Right+(br?1:0),rect.Top-1);
            if(bb) gr.DrawLine(p,rect.Left-(bl?1:0),rect.Bottom+1,rect.Right+(br?1:0),rect.Bottom+1);
          }
          e.HasMorePages=PrintNumber<PrintMulti;
        }
				internal void MopUndo() { 
				  bool inmop=mop!=MouseOp.None;
				  if(mopundo&&inmop) return;
					PushUndo();
					mopundo=inmop;
				}
        internal void PushUndo(string code) { PushUndo(false,code);}
        internal void PushUndo(bool direct) { PushUndo(direct,null);}
        internal void PushUndo() { PushUndo(false,null);}
        internal void PushUndo(bool direct,string code) {
          if(undomap!=null&&code!=null&&code==undocode) return;
          NoMovePaste();
          if(map==null) return;
          undomap=direct?map:map.Clone();
          redomap=null;redosel=null;
          undosel=R.Copy(Selection);
          undocode=code;
          SetDirty(); 
        }
        internal bool Undo4Move() {
          if(MovePaste) return false;
          PushUndo();MovePaste=true;
          return true;
        }
        internal void Undo() { Undo(true);}
        internal void Undo(bool repaint) {
          if(undomap!=null&&undomap!=map) {
            bool resize=undomap.Width!=map.Width||undomap.Height!=map.Height;
            redomap=map;
            redosel=Selection;
            Selection=undosel;
            map=undomap;
            undomap=null;undocode=null;undosel=null;
            MovePaste=false;MoveBits=null;
            if(resize) UpdateBitmap();
            if(repaint) Repaint(true);            
          }        
        }
        internal void Redo() {
          if(redomap!=null&&redomap!=map) {
					  bool resize=redomap.Width!=map.Width||redomap.Height!=map.Height;
            undomap=map;undocode=null;
            undosel=Selection;
            Selection=redosel;
            map=redomap;
            redomap=null;
						if(resize) UpdateBitmap();
            Repaint(true);
          }
        }
        void UndoCode() { undocode=null;}
        internal void ClearUndo() { undomap=redomap=null;undocode=null;}
        private void miCommand_Click(object sender, EventArgs e) {
          ToolStripDropDownItem mi=sender as ToolStripDropDownItem;
          string cmd=mi.Tag as string;
					if(cmd=="") cmd=mi.Name.ToLower().Replace("&","");
          bool shift=GDI.ShiftKey,ctrl=GDI.CtrlKey;
          if(shift||ctrl) 
            switch(cmd) {
             case "expand":if(shift) cmd+=" x8";if(ctrl) cmd+=" diff";break;
             case "shrink":if(shift||ctrl) cmd="extend";break;
             case "bright":if(shift||ctrl) cmd="dark";break;
             case "nowhite":if(shift||ctrl) cmd="noblack";break;
             case "remove_dots":cmd="remove_dots"+(ctrl?shift?"":" bl":shift?" wh":" bl wh");break;
						 case "bw":cmd=ctrl?"bw 128 max":shift?"bw 128":"bw 128 min";break;
            }
          ProcessCommand(cmd);
        }
        bool HasParam(string[] cmd,string param) {
          return Array.IndexOf(cmd,param)>=0;
        }        
        int HasParams(string[] cmd,int start,params string[] param) {
          for(int i=0;i<param.Length;i++)
            if(Array.IndexOf(cmd,param[i],start,cmd.Length-start)>=0) return i+1;
          return 0;
        }
        int IntParam(string[] cmd,int idx,int def) {
          int x;
          if(idx<0||idx>=cmd.Length) return def;
          string s=cmd[idx];
          if(s.StartsWith("--")) s=s.Substring(2);
          return int.TryParse(cmd[idx],out x)?x:def;
        }
        int Alpha(string[] cmd) {
          int l=cmd.Length,a;
          if(l<2||!cmd[l-1].StartsWith("alpha=")) return 0;
          int.TryParse(cmd[l-1].Substring(6),out a);
          return a;
        }
        public bool ProcessCommand(string cmd) {
          if(cmd==null) return false;
          string[] sa=cmd.Split(new char[] {' '},StringSplitOptions.RemoveEmptyEntries);
          cmd=sa[0].ToLower();
          int i=0;
          switch(cmd) {
           case "print":Print();break;
           case "new":NewFile();break;
           case "reload":Reload();break;
           case "save":SaveFile(HasParam(sa,"as"),HasParam(sa,"icon"));break;
           case "fillbox":{ int dx=4,dy=4;if(!IsSelectionEmpty()) {dx=1+Math.Abs(Selection[0]-Selection[2]);dy=1+Math.Abs(Selection[1]-Selection[3]);if(dx<2&&dy<2) dx=dy=4;};FillDiff(false,dx,dy);} break;
           case "filldiff":FillDiff(HasParam(sa,"repl"),0,0);break;
           case "satur":if(HasParam(sa,"2")) Satur2(HasParam(sa,"desat"),HasParam(sa,"over"),Alpha(sa)); else Filter(FilterOp.Saturate,0,false);break;
           case "gamma":Gamma(HasParam(sa,"inv"),sa.Length>1?X.t(sa[1],0.5):0.5,0);break;
           case "gammai":Gammai(HasParam(sa,"inv"),sa.Length>1?X.t(sa[1],0.5):0.5,0);break;
           case "paln":PalN(IntParam(sa,1,0),HasParam(sa,"avg"),Alpha(sa));break;
           case "bold":Bold(false,HasParam(sa,"max"),HasParam(sa,"vert"));break;
           case "comp":Comp(HasParam(sa,"hori"),HasParam(sa,"vert"),3);break;
           case "difpal":DiffusePal(IntParam(sa,1,16),Palette.pal16);break;
           case "diffuse":
           case "matrix":Matrix(cmd[0]=='d',HasParam(sa,"rgb"),HasParam(sa,"abs"),IntParam(sa,1,0));break;
           case "chan":Filter(FilterOp.Channel,IntParam(sa,1,0),false);break;
           case "bgr":Filter(FilterOp.Perm,IntParam(sa,1,0x000102),false);break;
           case "knee":Knee(HasParam(sa,"w")?2:HasParam(sa,"c")?1:0,HasParam(sa,"x"),HasParam(sa,"y"),HasParam(sa,"o"));break;
           case "strip":Knee(3,HasParam(sa,"h"),HasParam(sa,"i"),false);break;
           case "cone":Knee(4,HasParam(sa,"h"),HasParam(sa,"i"),false);break;
           case "average":Knee(7,HasParam(sa,"h"),HasParam(sa,"v"),false);break;
           case "pixels":{ int size=IntParam(sa,1,8)/2-1;Knee(8,0!=(size&1),0!=(size&2),0!=(size&4));} break;
           case "erase":if(HasParam(sa,"/")||HasParam(sa,"\\")) Knee(6,HasParam(sa,"\\"),false,false); else Knee(5,HasParam(sa,"x"),HasParam(sa,"y"),false);break;
           case "paper":Paper(HasParam(sa,"xy"),(int)Board.papW.Value,HasParam(sa,"black")?0:Color1,HasParam(sa,"transp")?-1:bmap.White,IntParam(sa,1,0));break;
           case "saw":Saw(IntParam(sa,1,0),Alpha(sa));break;
           case "pal":Pal(sa[1].Replace("@f",""+Color1).Replace("@b",""+Color2),Alpha(sa));break;
           case "pal1":Pal1(sa[1].Replace("@f",""+Color1).Replace("@b",""+Color2),Alpha(sa));break;
           case "c4":C4(IntParam(sa,1,0),IntParam(sa,2,0),IntParam(sa,3,0),IntParam(sa,4,0));break;
           case "replacec":ReplaceC(Palette.Parse(sa[1].Replace("@f",""+Color1)),Palette.Parse(sa[2].Replace("@b",""+Color2)),HasParam(sa,"a")?128:0);break;
           case "sub":Filter(FilterOp.Substract,Palette.Parse(sa[1].Replace("@f",""+Color1).Replace("@b",""+Color2))|(((HasParam(sa,"i")?3:HasParam(sa,"g")?2:HasParam(sa,"w")?1:0)|(HasParam(sa,"s")?16:0))<<24),true);break;
           case "hue":Filter(FilterOp.Hue,IntParam(sa,1,4096),true);break;
					 case "invert":Filter(HasParam(sa,"intensity")?FilterOp.InvertIntensity:FilterOp.Invert,0,HasParam(sa,"bw"));break;
           case "towhite":Filter(FilterOp.ToWhite,IntParam(sa,1,4),false);break;
           case "toblack":Filter(FilterOp.ToBlack,IntParam(sa,1,4),false);break;
           case "emboss":Filter33(bmap.Filter33Emboss,sa.Length>1?X.t(sa[1],4):4,!HasParam(sa,"bw"));break;
           case "blur":Filter33(bmap.Filter33Blur,null,!HasParam(sa,"bw"));break;
           case "sharp":Filter33(bmap.Filter33Sharp,null,!HasParam(sa,"bw"));break;
           case "edge":Filter33(bmap.Filter33Edge,4,!HasParam(sa,"bw"));break;
           case "edge1":Filter33(bmap.Filter33Edge1,4,!HasParam(sa,"bw"));break;
           case "neq":Filter33(bmap.Filter33Neq,HasParam(sa,"x8")?"":null,HasParam(sa,"bw"));break;
           case "neq2":Neq(IntParam(sa,1,0),true,HasParam(sa,"gt"),HasParam(sa,"x8"),FillD8,IntParam(sa,2,0),IntParam(sa,3,0),bmap.White);break;
           case "hdr":Filter33(bmap.Filter33HDR,HasParam(sa,"x8")?"":null,HasParam(sa,"bw"));break;
           case "hdr4":hdr4(5,HasParam(sa,"rgb"),HasParam(sa,"satur"),HasParam(sa,"x8"),2);break; 
           case "c256":Color256(HasParam(sa,"8")?8:HasParam(sa,"64")?6:HasParam(sa,"4k")?4:HasParam(sa,"512")?3:0);break;           
           case "maxcount":MaxCount(IntParam(sa,1,256),HasParam(sa,"max"));break;           
           case "c765":Color765(HasParam(sa,"rgb")?0:HasParam(sa,"satur2")?3:HasParam(sa,"satur")?2:1);break;
           case "avg":Blur(IntParam(sa,1,4),true,true,IntParam(sa,2,0),IntParam(sa,3,4),IntParam(sa,4,-1));break;
           case "shadow":Shadow(IntParam(sa,1,4),-1);break;
           case "fall":Fall(IntParam(sa,1,bmap.White),IntParam(sa,2,0),HasParam(sa,"x8"),HasParam(sa,"exp"));break;
					 case "contrast":Filter(FilterOp.Contrast,0,HasParam(sa,"rgb"));break;
					 case "border":if(sa.Length>1) int.TryParse(sa[1],out i);Filter(FilterOp.Border,i,HasParam(sa,"x8"));break;
					 case "remove_dots":RemoveDots(HasParam(sa,"bl"),HasParam(sa,"wh"),"438"[HasParams(sa,1,"3","8")]);break;
					 case "remove_dust":{
             bool bl=HasParam(sa,"bl"),wh=HasParam(sa,"wh");
             if(sa.Length>1) int.TryParse(sa[1],out i);
             RemoveDust(i,bl,wh);
             } break;
           //case "replace":Filter1(bmap.Filter1Replace,new int[] {Color1,HasParam(sa,"bl")?0:HasParam(sa,"wh")?bmap.White:Color2});break;
           //int replace=mode==1?Color1:mode==2?Color2:mode==3?0:bmap.White;
           case "replace":Replace("G",HasParam(sa,"bl2")?0:HasParam(sa,"wh2")?bmap.White:Color1,HasParam(sa,"bl")?0:HasParam(sa,"wh")?bmap.White:Color2,0,0,0);break;           
           case "replacediff":Replace("",IntParam(sa,1,Color1),IntParam(sa,2,Color2),IntParam(sa,3,0),IntParam(sa,4,0),0);break;
           case "roof":Replace("",-1,0,100,0,1);break;
           case "remove":RemoveColor(HasParam(sa,"bl")?0:Color1,GDI.ShiftKey^Board.X8.Checked,HasParam(sa,"repeat"));break;
           case "erasec":EraseColor(HasParam(sa,"v"),HasParam(sa,"h"));break;
					 case "shape":ShapeCmd(sa.Length>1?sa[1]:"rectangle");break;
					 case "brush":BrushCmd(sa.Length>1?sa[1]:null);break;
					 case "help":HelpCmd();break;
					 case "board":BoardCmd();break;
					 case "rgb":RGBShift(HasParam(sa,"cmy"),HasParam(sa,"back"),HasParam(sa,"inv"));break;
           case "undo":Undo();break;
					 case "redo":Redo();break;
					 case "white":Clear(true);break;
           case "clear":Clear(false);break;
           case "screen":PrintScreen(IntParam(sa,1,1000),sa.Length>2?sa[2]:"");break;
           case "shrink":Shrink();break;
           case "extend":Extend();break;
           case "bright":Bright(false,HasParam(sa,"bw"),192);break;
           case "dark":Bright(true,HasParam(sa,"bw"),192);break;
           case "nowhite":NoWhite(true,false);break;
           case "noblack":NoWhite(false,true);break;
           case "levels":Filter(FilterOp.Levels,X.t(sa[1],16),true);break;
           case "strips":Filter(FilterOp.Strips,X.t(sa[1],16),true);break;
           case "paste":
             if(IsSelectionMode())
               PasteSelection(null,GDI.ShiftKey^Board.pasteTrans.Checked?Color2:-1,GDI.ShiftRKey^Board.pasteTRX.Checked,Board.GetDiff(),GDI.ScrollLock^Board.pasteRepeat.Checked,Board.pasteQuad.Checked,Board.pasteExtend.Checked,Board.GetMix(),Board.GetPasteFilter(),0);
             else LoadFile("",true,0,false);
             break;
           case "copy":if(IsSelectionMode()) CopySelection();else Clipboard.SetImage(GDI.ShiftKey?GetIcon(128,128,true,Board.GetIconTrColor(false)):bm);break;
           case "contour":Contour(HasParam(sa,"stroke"),HasParam(sa,"fill"),HasParam(sa,"inv"));break;
           case "whiteonly":Board.DrawWOnly.Checked^=true;break;
           case "expand":Expand(HasParam(sa,"x8"),HasParam(sa,"wonly"),HasParam(sa,"diff")?1:HasParam(sa,"white")?2:0);break;
           case "roll":Roll(IntParam(sa,1,0),IntParam(sa,2,0));break;
           case "half":Half(IntParam(sa,1,0),HasParam(sa,"back"),HasParam(sa,"white"),HasParam(sa,"black"),HasParam(sa,"min"),HasParam(sa,"max"),HasParam(sa,"vert"),HasParam(sa,"hori"));break;
           case "outline":Replace(bmap.White,Board.X8.Checked,-1,HasParam(sa,"bl")?0:Color1,-1);break;
           case "impand":Impand(int.Parse(sa[1]),int.Parse(sa[2]),HasParam(sa,"repeat"));break;
           case "bwa":FilterRect(1);break;
           case "c8a":FilterRect(3);break;
           case "grx":FilterRect(256+IntParam(sa,1,2));break;
           case "gro":FilterRect(512+IntParam(sa,1,2));break;
           case "pattern":BWPattern(Color1,sa[1],IntParam(sa,2,2),IntParam(sa,3,1),HasParam(sa,"inv"));break;
           case "c8":PushUndo();{int[] r=Rect();map.C8(r[0],r[1],r[2],r[3],IntParam(sa,1,128));} Repaint(true);break;
					 case "bw":PushUndo();{int[] r=Rect();map.BW(r[0],r[1],r[2],r[3],HasParam(sa,"min")?-1:HasParam(sa,"max")?1:HasParam(sa,"sqr")?2:HasParam(sa,"eye")?3:0,X.t(sa[1],128));} Repaint(true);break;
           case "gray":PushUndo();{int[] r=Rect();map.Gray(r[0],r[1],r[2],r[3],HasParam(sa,"min")?-1:HasParam(sa,"max")?1:HasParam(sa,"sqr")?2:HasParam(sa,"eye")?3:0);} Repaint(true);break;
           case "text":DrawText(0,0,IX(lmx,lmy),IY(lmx,lmy));break;
           case "tess":Tessel(IntParam(sa,1,0),IntParam(sa,2,0),HasParam(sa,"mima3"));break;
					 default:return false;
          }
					return true;
        }
				int FillD8 { get {return Board.DX1.Checked?1:Board.DX3.Checked?3:2;}}
				bool X8 { get {return Board.X8.Checked;}}
				bool Fill2Black { get {return Board.Fill2Black.Checked;}}
				bool FillNoBlack { get {return Board.FillNoBlack.Checked;}}
				bool DrawBlack { get {return Board.DrawBlack.Checked;}}
				bool DrawCenter { get {return Board.DrawCenter.Checked;}}
				bool DrawOrto { get {return Board.DrawOrto.Checked;}}
				bool DrawFilled { get {return Board.DrawFilled.Checked;}}
				bool ShapeMirrorX { get {return Board.ShapeMirrorX.Checked;}}
				bool ShapeMirrorY { get {return Board.ShapeMirrorY.Checked;}}
				bool ShapeAdjust { get {return Board.ShapeAdjust.Checked;}}
				bool BrushWhiteOnly { get {return Board.DrawWOnly.Checked;}}
				int GradMode { get { return Board.cbFill.SelectedIndex;}}           

				void ShapeCmd(string shape) {
          if(RBop!=MouseOp.DrawPolar&&RBop!=MouseOp.DrawRect&&RBop!=MouseOp.DrawEdge)
            SetMouseOp(MouseButtons.Right,MouseOp.DrawPolar);
          SetShape(shape);
				}
				void BrushCmd(string brush) {
          if(string.IsNullOrEmpty(brush)||brush=="1")
            DrawBrush=null;
          else {
            string[] sa=brush.Split(',');
            brush=sa.Length>1&&(GDI.ShiftKey||GDI.CtrlKey)?sa[1]:sa[0];
            DrawBrush=new bmap(brush);
          }
				}
				void HelpCmd() {
				  if(Help==null) {
            string file=GetType().Assembly.Location;
            file=file.Substring(0,file.Length-3)+"rtf";
            if(!File.Exists(file)) return;
            Help=new fHelp(file);
          }  
					Help.ShowDialog(this);
				}
				void BoardCmd() { BoardCmd(-1);}
				void BoardCmd(int tab) {
          if(mop!=MouseOp.None) return;
          int sel=Board.tabControl1.SelectedIndex;          
				  if(Board.Visible&&(tab<0||tab==sel)) { Board.Hide();return;}
          if(tab>=0) Board.tabControl1.SelectTab(tab);
          if(Board.Visible) return;
          Point pt=Cursor.Position;
          Board.Left=pt.X-Board.Width/2;
					Board.Top=pt.Y-Board.Height/2;
					if(Board.Left<0) Board.Left=0;
					if(Board.Top<0) Board.Top=0;
					Rectangle ps=Screen.PrimaryScreen.Bounds;
					if(Board.Left+Board.Width>ps.Width) Board.Left=ps.Width-Board.Width;
					if(Board.Top+Board.Height>ps.Height) Board.Top=ps.Height-Board.Height;
					Board.Show(this);
				}
        void Roll(int dx,int dy) {
          PushUndo();int[] r=Rect();map.Roll(dx,dy,r[0],r[1],r[2],r[3]);Repaint(true);
        }
        void Half(int rat,bool back,bool white,bool black,bool min,bool max,bool vert,bool hori) {
          PushUndo(true);
          bmap.HalfFunc f=bmap.HalfAvg;
          int m=0;
          if(back||black||white) {
            f=new bmap.HalfBack(128,black?0:white?bmap.White:Color1).Func;
          } else if(min||max) {
            f=bmap.HalfIMin;m=max?1:0;
          }
          if(rat==4)
            map=map.Half34(null,vert,hori,f,m);
          else if(rat==3)
            map=map.Half23(null,vert,hori,f,m);
          else
            map=map.Half(null,vert,hori,f,m,rat==2?5:rat+2);
          
          
          UpdateBitmap();
          Repaint(true);
        }
        void Expand(bool x8,bool wonly,int mode) {
          PushUndo();
          if(mode==1) map.Expand(x8,wonly,undomap,Rect(),0,bmap.White,-1,-1);
          else if(mode==2) map.Expand(x8,wonly,undomap,Rect(),bmap.White,-1,0,-1);
          else if(mode==3) map.Expand(x8,wonly,undomap,Rect(),bmap.White,-1,-1,0);
          else map.Expand(x8,wonly,undomap,Rect(),bmap.Black,-1,-1,bmap.Black);
          Repaint(true);
        }
        void Impand(int color,int color2,bool repeat) {
          PushUndo("Impand");
          map.Impand(Rect(),color,color2,15,repeat);
          Repaint(true);
        }
        void Tessel(int size,int shape,bool mima3) {
          PushUndo();
          Tess.Tessel(map.Width-2,map.Height-2,map.Width,map.Data,map.Width+1,size,shape,mima3);
          Repaint(true); 
        }
        private void fMain_Resize(object sender,EventArgs e) {
          Repaint(false);
        }

        protected override void OnPaintBackground(PaintEventArgs e) {
          //base.OnPaintBackground(e);
        }
        protected override void OnPaint(PaintEventArgs e) {
          //base.OnPaint(e);
          //Repaint(e.Graphics,e.ClipRectangle.Left,e.ClipRectangle.Top,e.ClipRectangle.Width,e.ClipRectangle.Height);
          timeDraw=true;
        }        

        private void timer_Tick(object sender,EventArgs e) {
          if(timeDraw) {
            Repaint(timeDirty);
            timeDraw=timeDirty=false;
          }
        }
        int ColorIdx(int idx,bool shift) {
          idx=idx*2+(shift?1:0)-1;
          switch(idx) {
           case 1:return 0xff0000;
           case 2:return 0xff0088;
           case 3:return 0xffff00;
           case 4:return 0xff8800;
           case 5:return 0x00ff00;
           case 6:return 0x88ff00;
           case 7:return 0x00ffff;
           case 8:return 0x00ff88;
           case 9:return 0x0000ff;
           case 10:return 0x0088ff;
           case 11:return 0xff00ff;
           case 12:return 0x8800ff;
           case 13:return 0xffffff;
           case 14:return 0xcccccc;
           case 15:return 0x888888;
           case 16:return 0x444444;
           default:return 0;
          }
        }
        static Color IntColor(int color) {return Color.FromArgb(color|(255<<24));}
        private void bColor_Click(object sender,EventArgs e) {
          return;
          //ColorClick(sender as Button,MouseButtons.Left);
        }
        internal void bColor_MouseDown(object sender,MouseEventArgs e) {
          Button b=sender as Button;
          if(b!=null) switch(b.Name) {
           case "bCLight1":DarkColor(false,true,GDI.CtrlKey?3:GDI.ShiftKey?2:1);return;
           case "bCLight2":DarkColor(true,true,GDI.CtrlKey?3:GDI.ShiftKey?2:1);return;
           case "bCDark1":DarkColor(false,false,GDI.CtrlKey?3:GDI.ShiftKey?2:1);return;
           case "bCDark2":DarkColor(true,false,GDI.CtrlKey?3:GDI.ShiftKey?2:1);return;
          }
          ColorClick(sender as Button,e.Button); 
        }        
        void ColorClick(Button b,MouseButtons button) {
          SetStatusBar(false);          
          int color;
          color=b.BackColor.ToArgb()&0xffffff;
          bool shift=GDI.ShiftKey;
          int rgbs=Palette.RGBSum(color);
          bool grx=b.Name.StartsWith("bColorGr");
          if(GDI.ShiftKey) color=Palette.ColorIntensity765(color,765-(765-rgbs)/2);
          else if(GDI.CtrlKey) color=Palette.ColorIntensity765(color,rgbs/2);
          if(!grx) UpdateGrx(color);
          /*} else {
            int j=b.Tag!=null?Convert.ToInt32(b.Tag):0;
            int i=GDI.CtrlKey?128:256;
            //if(GDI.ShiftKey) i-=64;
            bool shift=GDI.ShiftKey;
            color=Palette.ColorIntensity(j>0?ColorIdx(j,shift):b.BackColor.ToArgb()&0xffffff,i);
          }*/
          if(button==MouseButtons.Right) {
            Color2=color;UpdateColors();
          } else {
            Color1=color;UpdateColors();
          }
        }
        internal void UpdateGrx(int color) {
          Board.bColorGr1.BackColor=bColorGr1.BackColor=Palette.IntColor(Palette.ColorIntensity765(color,765*8/9));
          Board.bColorGr2.BackColor=bColorGr2.BackColor=Palette.IntColor(Palette.ColorIntensity765(color,765*7/9));
          Board.bColorGr3.BackColor=bColorGr3.BackColor=Palette.IntColor(Palette.ColorIntensity765(color,765*6/9));
          Board.bColorGr4.BackColor=bColorGr4.BackColor=Palette.IntColor(Palette.ColorIntensity765(color,765*5/9));
          Board.bColorGr5.BackColor=bColorGr5.BackColor=Palette.IntColor(Palette.ColorIntensity765(color,765*4/9));
          Board.bColorGr6.BackColor=bColorGr6.BackColor=Palette.IntColor(Palette.ColorIntensity765(color,765*3/9));
          Board.bColorGr7.BackColor=bColorGr7.BackColor=Palette.IntColor(Palette.ColorIntensity765(color,765*2/9));
          Board.bColorGr8.BackColor=bColorGr8.BackColor=Palette.IntColor(Palette.ColorIntensity765(color,765*1/9));
        }
        internal void UpdateGrx(int color,int color2) {
          Board.bColorGr1.BackColor=bColorGr1.BackColor=Palette.IntColor(Palette.RGBMix(color,color2,1,10));
          Board.bColorGr2.BackColor=bColorGr2.BackColor=Palette.IntColor(Palette.RGBMix(color,color2,2,10));
          Board.bColorGr3.BackColor=bColorGr3.BackColor=Palette.IntColor(Palette.RGBMix(color,color2,3,10));
          Board.bColorGr4.BackColor=bColorGr4.BackColor=Palette.IntColor(Palette.RGBMix(color,color2,4,10));
          Board.bColorGr5.BackColor=bColorGr5.BackColor=Palette.IntColor(Palette.RGBMix(color,color2,5,10));
          Board.bColorGr6.BackColor=bColorGr6.BackColor=Palette.IntColor(Palette.RGBMix(color,color2,6,10));
          Board.bColorGr7.BackColor=bColorGr7.BackColor=Palette.IntColor(Palette.RGBMix(color,color2,7,10));
          Board.bColorGr8.BackColor=bColorGr8.BackColor=Palette.IntColor(Palette.RGBMix(color,color2,8,10));
        }
        internal void bGradMode_Click(object sender, EventArgs e) { bGradMode_Click(sender,MouseButtons.Left);}
        internal void bGradMode_Click(object sender, MouseButtons mb) {
          Control c=sender as Control;
          ToolStripDropDownItem m=sender as ToolStripDropDownItem;
          string s=(m!=null?m.Tag:c!=null?c.Tag:"") as string;
          if(s.Contains(" ")) {
            string[] sa=s.Split(' ');
            bool ctrl=GDI.CtrlKey,shift=GDI.ShiftKey;
            s=sa[sa.Length>2&&shift?2:sa.Length>1&&(ctrl||shift&&sa.Length==2)?1:0];
          }
          
          int x;
          int.TryParse(s,out x);
          if(x==-5) {
            SetMouseOp(mb,MouseOp.Select);
          } else if(x==-4) { 
            Board.X8.Checked^=true;
            if(!IsMouseOp(MouseOp.FillFlood)||!IsMouseOp(MouseOp.FillBorder))
              SetMouseOp(mb,MouseOp.FillFlood);
          } else if(x<0) {
            SetMouseOp(mb,x==-1?MouseOp.FillShape:x==-2?MouseOp.FillFlood:x==-6?MouseOp.FillLinear
             :x==-7?MouseOp.FillRadial:x==-8?MouseOp.FillVect:x==-9?MouseOp.Fill4:x==-10?MouseOp.FillSquare:x==-11?MouseOp.FillFloat:MouseOp.FillBorder); 
          } else {          
            Board.cbFill.SelectedIndex=x;
          }
          UpdateMode();
        }
        Color XOpBack=Color.Wheat;
        void UpdateMode() {
          mFillCircle.Checked=GradMode==0;
          mFillDiamond.Checked=GradMode==1;
          mFillSquare.Checked=GradMode==2;
          mFillHorizont.Checked=GradMode==3;
          mFillVertical.Checked=GradMode==4;
          mFillRaise.Checked=GradMode==5;
          mFillFall.Checked=GradMode==6;
                    
          miDrawFree.Checked=RBop==MouseOp.DrawFree;
          miDrawFree.BackColor=LBop==MouseOp.DrawFree?XOpBack:SystemColors.Control;
          miDrawLine.Checked=RBop==MouseOp.DrawLine;
          miDrawLine.BackColor=LBop==MouseOp.DrawLine?XOpBack:SystemColors.Control;
          miDrawPolar.Checked=RBop==MouseOp.DrawPolar;
          miDrawPolar.BackColor=LBop==MouseOp.DrawPolar?XOpBack:SystemColors.Control;
          miDrawRect.Checked=RBop==MouseOp.DrawRect;
          miDrawRect.BackColor=LBop==MouseOp.DrawRect?XOpBack:SystemColors.Control;
          miDrawEdge.Checked=RBop==MouseOp.DrawEdge;
          miDrawEdge.BackColor=LBop==MouseOp.DrawEdge?XOpBack:SystemColors.Control;
          miDrawMorph.Checked=RBop==MouseOp.DrawMorph;
          miDrawMorph.BackColor=LBop==MouseOp.DrawMorph?XOpBack:SystemColors.Control;

          mFillLinear.Checked=IsMouseOp(MouseOp.FillLinear);
          mFillFlood.Checked=IsMouseOp(MouseOp.FillFlood);
          mFillBorder.Checked=IsMouseOp(MouseOp.FillBorder);
          miEditSelect.Checked=IsMouseOp(MouseOp.Select);
          miFillReplace.Checked=IsMouseOp(MouseOp.Replace);
          
          Board.bModeSelect.BackColor=IsMouseOp(MouseOp.Select)?Color.White:SystemColors.Control;          
          Board.bModeCircle.BackColor=bModeCircle.BackColor=IsMouseOp(MouseOp.FillShape)?Color.White:SystemColors.Control;
          Board.bModeLinear.BackColor=bModeLinear.BackColor=IsMouseOp(MouseOp.FillLinear)?Color.White:SystemColors.Control;
          Board.bModeFill.BackColor=bModeFill.BackColor=IsMouseOp(MouseOp.FillFlood)?Color.White:SystemColors.Control;
          Board.bModeBorder.BackColor=IsMouseOp(MouseOp.FillBorder)?Color.White:SystemColors.Control;
          Board.bModeRadial.BackColor=IsMouseOp(MouseOp.FillRadial)?Color.White:SystemColors.Control;
          Board.bModeSquare.BackColor=IsMouseOp(MouseOp.FillSquare)?Color.White:SystemColors.Control;
          Board.bModeFloat.BackColor=IsMouseOp(MouseOp.FillFloat)?Color.White:SystemColors.Control;
          Board.cbSquare.Visible=IsMouseOp(MouseOp.FillSquare);

          Board.bDrawFree.BackColor=IsMouseOp(MouseOp.DrawFree)?Color.White:SystemColors.Control;
          Board.bDrawLine.BackColor=IsMouseOp(MouseOp.DrawLine)?Color.White:SystemColors.Control;
          Board.bDrawRect.BackColor=IsMouseOp(MouseOp.DrawRect)?Color.White:SystemColors.Control;
          Board.bDrawPolar.BackColor=IsMouseOp(MouseOp.DrawPolar)?Color.White:SystemColors.Control;
          Board.bDrawEdge.BackColor=IsMouseOp(MouseOp.DrawEdge)?Color.White:SystemColors.Control;
          Board.bDrawMorph.BackColor=IsMouseOp(MouseOp.DrawMorph)?Color.White:SystemColors.Control;
          Board.bDrawReplace.BackColor=IsMouseOp(MouseOp.Replace)?Color.White:SystemColors.Control;
        }

        void ChooseColor(bool color2) { ChooseColor(color2?bColor2:bColor1);}
        public void ChooseColor(Button b) {
          CDialog.Color=b.BackColor;
          CDialog.FullOpen=true;
          if(DialogResult.OK==CDialog.ShowDialog(this)) {
            int rgb=CDialog.Color.ToArgb()&0xffffff;
            UpdateGrx(rgb);
            if(b==bColor1) Color1=rgb;
            else if(b==bColor2) Color2=rgb;
            else b.BackColor=CDialog.Color;
            UpdateColors();
            int[] cc=CDialog.CustomColors;
            int h,c=(CDialog.Color.B<<16)|(CDialog.Color.G<<8)|CDialog.Color.R;
            if(cc[0]==c) return;
            for(h=0;h<cc.Length-1;h++)
              if(cc[h]==c) break;
            while(h>0) {
              cc[h]=cc[h-1];
              h--;
            }
            cc[0]=c;  
            CDialog.CustomColors=cc;            
          }        
        }
        internal void bColor_Click2(object sender,EventArgs e) {
          Button b=sender as Button;
          ChooseColor(b.Name=="bColor2");
        }

        internal void bColor2_MouseDown(object sender, MouseEventArgs e) {
          if(e.Button==MouseButtons.Right) {
            bool b2=sender==bColor1||sender==Board.bColor1;
            if(b2) {
              Color2=Color1;
              Board.bColor2.BackColor=bColor2.BackColor=bColor1.BackColor;
            } else {
              Color1=Color2;
              Board.bColor1.BackColor=bColor1.BackColor=bColor2.BackColor;
            }
            if(GDI.CtrlKey||GDI.ShiftKey) ChooseColor(!b2);
          }  
        }

    internal void bSwap_Click(object sender, EventArgs e) {
          SwapColors();
        }
        
        void SwapColors() {
          int r=Color1;
          Color1=Color2;Color2=r;
          UpdateGrx(Color1,Color2);
          UpdateColors2();
        }
        void UpdateColors2() {
          UpdateColors();
          SetStatusBar(false);
        }

        internal void DarkColor(bool color2,bool light,int mul) {
          int step=48;
          if(mul>1) step*=mul;
          int r=color2?Color2:Color1;
          r=Palette.ColorIntensity765(r,Palette.RGBSum(r)+(light?step:-step));
          //r=Palette.Gamma(r,light?1.5:0.75,true);
          if(color2) Color2=r;else Color1=r;
          UpdateColors2();
        }
        

        internal void bClear_Click(object sender,EventArgs e) {
          Clear(GDI.CtrlKey);
        }

        private void panel_MouseUp(object sender,MouseEventArgs e) {
          AnchorStyles anch=panel.Anchor;
          Point l=panel.Location;
          if(e.X<0) {
            l.X=0;
            anch=anch&~AnchorStyles.Right|AnchorStyles.Left;
          } else if(e.X>panel.Width) {
            l.X=ClientSize.Width-panel.Width;
            anch=anch&~AnchorStyles.Left|AnchorStyles.Right;
          }
          if(e.Y<0) {
            l.Y=0;
            anch=anch&~AnchorStyles.Bottom|AnchorStyles.Top;
          } else if(e.Y>panel.Height) {
            l.Y=ClientSize.Height-panel.Height;
            anch=anch&~AnchorStyles.Top|AnchorStyles.Bottom;
          }
          if(l.X!=panel.Left||l.Y!=panel.Top) panel.Location=l;
          if(anch!=panel.Anchor) panel.Anchor=anch;
          else SetStatusBar(bClear.Visible);
        }

        
        void ChangeFileName(string filename) {
          ofd.FileName=filename;
          string fn=Path.GetFileName(filename);
          Dirty=false;
          Text="Fill"+(string.IsNullOrEmpty(fn)?"":" - "+fn);//+(Dirty?"*":"");
        }
        public static bool IsUrl(string name) {
          if(name+""=="") return false;
          return Regex.IsMatch(name,@"^(https?|file):",RegexOptions.IgnoreCase|RegexOptions.ExplicitCapture);
        }
        public static bool IsFile(string name,bool exists) {
          if(name+""=="") return false;
          return Regex.IsMatch(name,@"(png|bmp|jpg|jpeg)$",RegexOptions.IgnoreCase|RegexOptions.ExplicitCapture)&&(!exists||File.Exists(name));
        }
        void ShowEx(string caption,Exception ex) {
          MessageBox.Show(this,ex.Message+"\n\n"+ex.StackTrace,""+caption!=""?caption:""+ex.GetType(),MessageBoxButtons.OK,MessageBoxIcon.Error);
        }
        Bitmap LoadUrl(string url) {
          Bitmap bm=null;
          WebRequest wr=WebRequest.Create(url); 
         try {
          using(WebResponse response = wr.GetResponse()) {
            HttpWebResponse hr=response as HttpWebResponse;
            Stream receiveStream = response.GetResponseStream();
            if(hr==null||hr.StatusCode==HttpStatusCode.OK)
              bm=Bitmap.FromStream(receiveStream) as Bitmap;
            response.Close();
          }
         } catch(Exception ex) {
           ShowEx(url,ex);
         }
          return bm;
        }
        Bitmap LoadBitmap(string name,bool hide,int sleep) {
          if(""+name==""||name==":") {
            Bitmap bm=Clipboard.GetImage() as Bitmap;
            if(bm!=null) return bm;
            string text=Clipboard.GetText();
            if(""+text=="") return null;
            if(IsUrl(text)) return LoadUrl(text);
            if(IsFile(text,true)) return Bitmap.FromFile(text) as Bitmap;
            return null;
          }
          if(name=="*"||name=="/")
            return LoadScreen(hide,sleep);
          if(IsUrl(name)) {
            return LoadUrl(name);
          }
          return Bitmap.FromFile(name) as Bitmap;
        }        
        bool LoadFile(string filename,bool update,int sleep,bool reload) {
          Bitmap x;
          bool undo=true;
          if(""+filename==""||filename==":"||IsUrl(filename)) {
            ofd.FileName=null;
            x=LoadBitmap(filename,false,0);
            if(x==null) return false;            
          } else if(filename=="*"||filename=="/") {
            x=LoadScreen(true,sleep);            
          } else {
            if(!File.Exists(filename)) {
              DialogResult dr=MessageBox.Show("File "+filename+" does not exists.\nCreate new ?","Fill",MessageBoxButtons.YesNoCancel,MessageBoxIcon.Warning,MessageBoxDefaultButton.Button1);            
              if(dr==DialogResult.Cancel) return false;
              NewFile();
              if(dr==DialogResult.Yes) {
                ChangeFileName(filename);
                SaveFile(false,false);
              }
              return true;
            } else {
#if GRC
						  if(IsGRC(filename)) {
							  x=LoadGRC(filename);
							} else 
#endif
                using(Bitmap ff=Bitmap.FromFile(filename) as Bitmap) {
								  Bitmap nbm=new Bitmap(ff.Width,ff.Height,PixelFormat.Format32bppRgb);
									nbm.SetResolution(ff.HorizontalResolution,ff.VerticalResolution);
									x=nbm;
									using(Graphics g=Graphics.FromImage(x)) {
                    if(0!=(ff.PixelFormat&PixelFormat.Alpha))
                      g.FillRectangle(new SolidBrush(IntColor(Color2)),0,0,ff.Width,ff.Height);
									  //g.DrawImage(ff,0,0,ff.Width,ff.Height);
										g.DrawImageUnscaled(ff,0,0);
									}
                }
            }
            if(!reload) {
              ChangeFileName(filename);
              ClearUndo();
              undo=false;
            }
          }
          if(undo) PushUndo(true);
          bm=x as Bitmap;
          if(bm==null) {
            bm=new Bitmap(x);
            x.Dispose();
          }
          map=bmap.FromBitmap(null,bm,Color2);
          if(update) { Repaint(true);}
          return true;
        }
        Bitmap LoadScreen(bool hide,int sleep) {
          bool visible=hide?this.Visible:false,board=Board.Visible;
          if(visible) this.Hide();
					if(board) Board.Hide();
          if(sleep>0) System.Threading.Thread.Sleep(sleep);
          Screen ps=Screen.PrimaryScreen;
          Bitmap bm=new Bitmap(ps.Bounds.Width,ps.Bounds.Height,PixelFormat.Format32bppRgb);
          Graphics gr=Graphics.FromImage(bm);
          gr.CopyFromScreen(ps.Bounds.X,ps.Bounds.Y,0,0,ps.Bounds.Size,CopyPixelOperation.SourceCopy);
          gr.Dispose();
					if(board) Board.Show();
          if(visible) this.Show();
          return bm;
        }
        void PrintScreen(int sleep,string param) {
          if(""+param=="") {LoadFile("*",true,sleep,false);return;}
          Bitmap bm=LoadScreen(true,sleep);
          bool b,t,l,r,p=l=r=b=t=false;
          foreach(char ch in param) 
            if(ch=='>') r=true;else if(ch=='v') b=true;else if(ch=='p') p=true;
          if(p) {
            if(!IsSelectionMode()) SetMouseOp(MouseButtons.Left,MouseOp.Select);
            PushUndo();
            int x=IX(lmx,lmy),y=IY(lmx,lmy); 
            Selection=new int[4] {x,y,x+bm.Width-1,y+bm.Height-1};
            map.CopyBitmap(bm,x+1,y+1,Board.pasteTrans.Checked?Color2:-1,Board.pasteTRX.Checked,Board.GetDiff(),Board.GetMix(),Board.GetPasteFilter());
            MoveBits=bm;MoveTrColor=Board.pasteTrans.Checked?Color2:-1;MoveXor=Board.GetDiff()>0;
            MovePaste=true;
            Repaint(true);
            return;
          }
          PushUndo(true);
          map=new bmap(map,0,0,map.Width-1+(l||r?bm.Width:0),map.Height-1+(t||b?bm.Height:0),0);
          if(l||r) { int x=l?1:map.Width-bm.Width-1; map.FillRectangle(x,bm.Height+1,x+bm.Width-1,map.Height-1,Color2);}
          if(t||b) { int y=t?1:map.Height-bm.Height-1; map.FillRectangle(bm.Width+1,y,map.Width-1,y+bm.Height-1,Color2);}
          map.CopyBitmap(bm,r?map.Width-bm.Width-1:1,b?map.Height-bm.Height-1:1,-1,false,0,0,-1);
          UpdateBitmap();
          Repaint(true);
        }
        void NewMap(bool update) {
          ClearUndo();
          int w,h;
          int.TryParse(Board.tNewWidth.Text,out w);
          int.TryParse(Board.tNewHeight.Text,out h);
          if(w<1) w=Screen.PrimaryScreen.Bounds.Width;
          if(h<1) h=Screen.PrimaryScreen.Bounds.Height;
          map=new bmap(w+2,h+2);
          map.Clear();
          if(update) {UpdateBitmap();Repaint(true);}
        }
				void Shrink2() {
				  int mx=IX(lmx,lmy),my=IY(lmx,lmy);
					bool empty=IsSelectionEmpty();
					if(!empty) { 
					  if(R.Inside(Selection,0,0,bm.Width-1,bm.Height-1)) Shrink();
						else {
						  DrawSelection();
						  R.Intersect(Selection,0,0,bm.Width-1,bm.Height-1);
						  DrawSelection();
					  }
            return;
				  }
					int x0=mx,x1=bm.Width-1-mx,y0=my,y1=bm.Height-1-my;
					if(x0<0||x1<0||y0<0||y1<0) return;
					int[] shrink=new int[] {0,0,bm.Width-1,bm.Height-1};
					if(x0<x1&&x0<y0&&x0<y1) shrink[0]=x0==0?1:mx;
					else if(x1<y0&&x1<y1) shrink[2]=x1==0?bm.Width-2:mx;
					else if(y0<y1) shrink[1]=y0==0?1:my;
					else shrink[3]=y1==0?bm.Height-2:my;
					Shrink(shrink);
				}
        void Extend() {
          bool empty=IsSelectionEmpty();
          int dx=empty?IX(lmx,lmy):Selection[0],dy=empty?IY(lmx,lmy):Selection[1];
          int dx2=(empty?dx:Selection[2])+2,dy2=(empty?dy:Selection[3])+2;
          bmap map2=map.Extend(dx,dy,dx2,dy2,Color2,true);
          if(map2==null) return;
          if(MoveBits!=null) {
            undomap=undomap.Extend(dx,dy,dx2,dy2,Color2,true);
            if(undosel!=null) {
              if(dx<0) { undosel[0]-=dx;undosel[2]-=dx;}
              if(dy<0) { undosel[1]-=dy;undosel[3]-=dy;}
            }
            map2.CopyBitmap(MoveBits,Selection[0]+1,Selection[1]+1,MoveTrColor,Board.pasteTRX.Checked,Board.GetDiff(),Board.GetMix(),Board.GetPasteFilter());
          } else {
            PushUndo(true);
          }
          map=map2;
          if(dx<0) { if(!empty) {Selection[0]-=dx;Selection[2]-=dx;} sx+=dx*zoom/ZoomBase;dx=0;}
          if(dy<0) { if(!empty) {Selection[1]-=dy;Selection[3]-=dy;} sy+=dy*zoom/ZoomBase;dy=0;}
          UpdateBitmap();
          Repaint(true);          
        }
        void Shrink() { Shrink(ClippedSelection());}
			  void Shrink(int[] sel) {
				  if(sel==null) return;
					int nw=sel[2]-sel[0]+3,nh=sel[3]-sel[1]+3;
					if(nw==map.Width&&nh==map.Height) return;
          bmap map2=new bmap(nw,nh);
          map2.CopyRectangle(map,sel[0]+1,sel[1]+1,sel[2]+1,sel[3]+1,1,1,-1);
          PushUndo(true);
          map=map2;
          UpdateBitmap();
          sx+=zoom*sel[0]/ZoomBase;sy+=zoom*sel[1]/ZoomBase;
          Selection[0]-=sel[0];Selection[1]-=sel[1];
          Selection[2]-=sel[0];Selection[3]-=sel[1];
          Repaint(true);
        }

        private void miDrawSwitch_Click(object sender, EventArgs e) {
          ToolStripMenuItem mi=sender as ToolStripMenuItem;
          if(mi.Name.EndsWith("DrawCenter")) {
            Board.DrawCenter.Checked^=true;
          } else if(mi.Name.EndsWith("DrawOrto")) {
            Board.DrawOrto.Checked^=true;
          } else if(mi.Name.EndsWith("DrawFilled")) {
            Board.DrawFilled.Checked^=true;
          } else {
            Board.DrawBlack.Checked^=true;
          }
          
        }
        Bitmap GetBitmap(int transparent) {
          Bitmap x=new Bitmap(map.Width-2,map.Height-2,transparent<0?PixelFormat.Format32bppRgb:PixelFormat.Format32bppArgb);
          bmap.ToBitmap(map,1,1,x,0,0,map.Width-2,map.Height-2,false);
          if(transparent>=0) x.MakeTransparent(Color.FromArgb(transparent));
          return x;
        }
        Bitmap GetIcon(int width,int height,bool aspect,int trcolor) {
          int w=width,h=height;
          if(aspect) {
            int w2=bm.Width*h/bm.Height;
            if(w2<h) w=w2;else h=bm.Height*w/bm.Width;
          }
          while(w>256||h>256) {w/=2;h/=2;}
          if(w<1||h<1) return null;
          bool resize=w!=bm.Width||h!=bm.Height;
          Bitmap x=GetBitmap(trcolor);
					if(!resize) return x;
          Bitmap res=new Bitmap(w,h,PixelFormat.Format32bppArgb);
          using(Graphics gr=Graphics.FromImage(res)) {
            gr.InterpolationMode=InterpolationMode.Bilinear;
            gr.SmoothingMode=SmoothingMode.HighQuality;
            gr.DrawImage(x,0,0,res.Width,res.Height);
          }
          x.Dispose();
          return res;
          /*BitmapData bd=bm.LockBits(new Rectangle(0,0,w,h),ImageLockMode.WriteOnly,PixelFormat.Format32bppArgb);
          int[] row=new int[w];
          for(int y=0;y<h;y++) {
            for(int x=0;x<w;x++)
              row[x]=0x70124589;
            Marshal.Copy(row,0,new IntPtr(bd.Scan0.ToInt64()+bd.Stride*y),bd.Width);
          }
          bm.UnlockBits(bd);          
           */          
        }
        

        private void miFileNew_Click(object sender, EventArgs e) {
          if(GDI.CtrlKey) Reload();
          else NewFile();
        }

        private void miBrush_Click(object sender, EventArgs e) {
          ToolStripDropDownItem mi=sender as ToolStripDropDownItem;
          string brush=mi.Tag as string;
          SetBrush(brush);
        }
        internal void SetBrush(string brush) {
          if(string.IsNullOrEmpty(brush)||brush=="1")
            DrawBrush=null;
          else {
            string[] sa=brush.Split(',');
            brush=sa.Length>1&&(GDI.ShiftKey||GDI.CtrlKey)?sa[1]:sa[0];
            DrawBrush=new bmap(brush);
          }          
        }

        internal void miDraw_Click(object sender, EventArgs e) { miDraw_Click(sender,GDI.ShiftKey?MouseButtons.Left:MouseButtons.Right);}
        internal void miDraw_Click(object sender, MouseButtons mb) {
          ToolStripDropDownItem mi=sender as ToolStripDropDownItem;
          Control c=sender as Control;
          string tag="";
          if(mi!=null) tag=""+mi.Tag;
          else if(c!=null) tag=""+c.Tag;

          MouseOp op;
          switch(tag) {
           case "F":op=MouseOp.DrawFree;break;
           case "L":op=MouseOp.DrawLine;break;
           case "P":op=MouseOp.DrawPolar;break;
           case "R":op=MouseOp.DrawRect;break;
           case "E":op=MouseOp.DrawEdge;break;
           case "M":op=MouseOp.DrawMorph;break;
           case "A":op=MouseOp.Select;SetMouseOp(mb==MouseButtons.Left?MouseButtons.Right:MouseButtons.Left,MouseOp.Replace);break;
           default:return;
          }
          SetMouseOp(mb,op);
        }
        bool IsStatusBar() { return tStatus.Visible;}
        bool StatusInfo(out int x,out int y,out int c,out int dx,out int dy) {
          x=y=c=dx=dy=-1;
          x=IX(lmx,lmy);y=IY(lmx,lmy);
          if(x<0||y<0||x>=bm.Width||y>=bm.Height) return false;
          c=map.Data[(y+1)*map.Width+x+1]&bmap.White;
          if(pmb!=MouseButtons.None||IsSelectionEmpty()) {
            dx=x-pmcx;dy=y-pmcy;
          } else {
            dx=Selection[2]-Selection[0];
            dy=Selection[3]-Selection[1];
          }
          if(!(movesel&&pmb!=MouseButtons.None)) {
            if(dx<0) dx=-dx;if(dy<0) dy=-dy;
            dx++;dy++;
          }
          x-=bx;y-=by;
          return true;
        }
        internal static string str(double x,string fmt) {
          return x.ToString("0.0",System.Globalization.CultureInfo.InvariantCulture);
        }
        string UpdateStatusBar() {
          int x,y,c,dx,dy;
          if(!StatusInfo(out x,out y,out c,out dx,out dy)) return "";
          string cstr=null;
           cstr=" #"+c.ToString("X6");
          string dstr;
          if(movesel&&pmb!=MouseButtons.None) {
            dstr=""+(dx>0?"+":"")+dx+","+(dy>0?"+":"")+dy;
          } else {
            dstr=""+dx+"x"+dy;
          }
          if(Board.chArea.Checked ) {
            cstr=(dx*dy).ToString(" (#,#)"); 
          } else if(Board.chRadial.Checked) {
            int xx=dx-1,yy=dy-1;
            cstr=str(Math.Sqrt(xx*xx+yy*yy),"0.0");
            if(xx!=0||yy!=0) {
              double a=Math.Atan2(dy,dx)*180/Math.PI;
              cstr+=" "+str(a,"0.0")+"\xb0";
            }
          } 
          string txt=""+x+","+y+" "+dstr+" "+cstr;
          tStatus.Text=txt;
          return txt;
        }
        void SetMouseOp(MouseButtons button,MouseOp op) {
          bool osel=IsSelectionMode();
          if(osel) DrawSelection();
          if(button==MouseButtons.Left) LBop=op;
          else if(button==MouseButtons.Right) RBop=op;
          else if(button==MouseButtons.Middle) MBOp=op;
          bool nsel=LBop==MouseOp.Select||RBop==MouseOp.Select;          
          DrawSelection();
          if(!IsSelectionMode()) Selection[0]=1+(Selection[2]=0);
          UpdateMode();
          SetStatusBar(op==MouseOp.Select||op>=MouseOp.DrawFree);
        }
        void SetStatusBar(bool show) {
          bool b=IsSelectionMode();
          tStatus.Visible=true;//show;
          bClear.Visible=bColor2.Visible=bColor1.Visible=bSwap.Visible=true;//!show;
        }


        private void miDrawMirror_Click(object sender, EventArgs e) {
          ToolStripDropDownItem mi=sender as ToolStripDropDownItem;
          string mode=mi.Tag as string;
          DrawMirror(mode=="x",mode=="y",mode=="a");
        }
        
        void DrawMirror(bool x,bool y,bool a) {
          if(x) Board.ShapeMirrorX.Checked^=true;
          if(y) Board.ShapeMirrorY.Checked^=true;
					if(a) Board.ShapeAdjust.Checked^=true;
        }

        static string ShapeByName(string name) {
          switch(name) {
           default:
           case "rectangle":return "-60,-60 -60,60 60,60 60,-60";
					 case "rounded":return "-50,-60 -58,-58 -60,-50 -60,50 -58,58 -50,60 50,60 58,58 60,50 60,-50 58,-58 50,-60";
           case "diamond":return "-60,0 0,60 60,0 0,-60";
           case "diamond2":return "-60,0 0,34 60,0 0,-34";
           case "triangle":return "-60,0 60,30 60,-30";
           case "triangle90": return "-60,0 0,60 60,0";
           case "triangle3":return "m-60,0 60,60 60,-60 -60,0 60,20 m-60,0 60,-20";
           case "valve":return "-60,-35 -60,35 0,0 60,35 60,-35 0,0";
           //case "arrow": return "-60,0 0,60 0,30 60,30 60,-30 0,-30 0,-60";
					 case "arrow": return "-90,0 -30,60 -30,30 90,30 90,-30 -30,-30 -30,-60";
					 case "arrow2": return "-90,0 -30,60 -30,30 30,30 30,60 90,00 30,-60 30,-30 -30,-30 -30,-60";
           case "hexagon": return "-60,0 -30,52 30,52 60,0 30,-52 -30,-52";
           case "hexagon2": return "-60,35 0,70 60,35 60,-35 0,-70 -60,-35";
           case "star6":return "-60,0 -30,17 -30,52 0,35 30,52 30,17 60,0 30,-17 30,-52 0,-35 -30,-52 -30,-17";
           case "circle":return "-60,5 -57,20 -50,34 -34,50 -20,57 -5,60 5,60 20,57 34,50 50,34 57,20 60,5 60,-5 57,-20 50,-34 34,-50 20,-57 5,-60 -5,-60 -20,-57 -34,-50 -50,-34 -57,-20 -60,-5"; 
           case "circle2":return "-60,0 -60,5 -57,20 -50,34 -34,50 -20,57 -5,60 5,60 20,57 34,50 50,34 57,20 60,5 60,0"; 
           case "star":return "-60,0 -14,0 0,42 14,0 60,0 22,-26 36,-68 0,-41 -36,-68 -22,-26"; 
           case "octa":return "-60,25 -25,60 25,60 60,25 60,-25 25,-60 -25,-60 -60,-25"; 
           case "octa2":return "-60,0 -42,42 0,60 42,42 60,0 42,-42 0,-60 -42,-42";
           case "star8":return "-30,0 -60,30 -30,30 -30,60 0,30 30,60 30,30 60,30 30,0 60,-30 30,-30 30,-60 0,-30 -30,-60 -30,-30 -60,-30";
           case "star12":return "-60,0 -40,20 -60,40 -40,40 -40,60 -20,40 0,60 20,40 40,60 40,40 60,40 40,20 60,0 40,-20 60,-40 40,-40 40,-60 20,-40 0,-60 -20,-40 -40,-60 -40,-40 -60,-40 -40,-20"; 
           case "star16":return "-40,-0 -60,20 -40,20 -60,40 -40,40 -40,60 -20,40 -20,60 0,40 20,60 20,40 40,60 40,40 60,40 40,20 60,20 40,0 60,-20 40,-20 60,-40 40,-40 40,-60 20,-40 20,-60 0,-40 -20,-60 -20,-40 -40,-60 -40,-40 -60,-40 -40,-20 -60,-20"; 
           case "penta": return "-60,0 -14,63 60,39 60,-39 -14,-63";
          }
        }
        void SetShape(string shname) {
          DrawShape=ShapeByName(shname);
        }
        private void miShape_Click(object sender, EventArgs e) {
          ToolStripDropDownItem mi=sender as ToolStripDropDownItem;
          if(RBop!=MouseOp.DrawPolar&&RBop!=MouseOp.DrawRect&&RBop!=MouseOp.DrawEdge)
            SetMouseOp(MouseButtons.Right,MouseOp.DrawPolar);
          string tag=mi.Tag as string;
          if(tag.IndexOf(',')>=0) {
            string[] sa=tag.Split(',');
            int i=0;
            if(GDI.ShiftKey) i=2;
            if(GDI.CtrlKey) i++;
            tag=sa[i<sa.Length?i:sa.Length-1];
          }
          SetShape(tag);
        }

        private void fMain_Deactivate(object sender, EventArgs e) {
          if(!string.IsNullOrEmpty(rtext)) {
           try { Clipboard.SetText(rtext);} catch { }
            rtext="";
          }

        }
        static float[] F4(float[] fa) {
          if(fa==null||fa.Length>3) return fa;
          float r0,r1,r2,r3;
          if(fa.Length==1) r0=r1=r2=r3=fa[0];
          else if(fa.Length==2) {r0=r3=fa[0];r1=r2=fa[1];}
          else if(fa.Length==3) {r0=fa[0];r1=r3=fa[1];r2=fa[2];}
          else {r0=fa[0];r1=fa[1];r2=fa[2];r3=fa[3];}
          return new float[] {r0,r1,r2,r3};
        }
        static float[] ParseFA(string s) {
          string[] sa=(""+s).Split(new string[] {" ",";",":"},StringSplitOptions.RemoveEmptyEntries);
          float[] fa=new float[sa.Length];
          for(int i=0;i<sa.Length;i++) 
            float.TryParse(sa[i].Replace(",","."),System.Globalization.NumberStyles.Float,System.Globalization.CultureInfo.InvariantCulture,out fa[i]);
          return fa;
        }
        static void SumFA(float[] fa) {
          if(fa==null||fa.Length<2) return;
          float sum=0;
          for(int i=0;i<fa.Length;i++) {
            sum+=fa[i];
            fa[i]=sum;
          }
        }

        void DrawShape2(bool shift,bool ctrl,bool alt) {
          if(IsSelectionEmpty()) return;
          PushUndo();
          int lwidth;lwidth=(int)Board.tbTLWidth2.Value;
          Pen p=new Pen(Board.blColor.BackColor,lwidth);
          SetDash(p);
          bool opaq=Board.chTOpaq.Checked;
          int x=Selection[0],y=Selection[1],x2=Selection[2],y2=Selection[3],w=x2-x+1,h=y2-y+1,r=w<h?w:h;
          Brush br=opaq?null:new SolidBrush(IntColor(Color2));
          using(Graphics gr=Graphics.FromImage(bm)) {
            GraphicsPath gp=null;
            if(alt) {
               gp=new GraphicsPath();
               if(w>=h) {
                 gp.AddLine(x+r/2,y,x2-r/2,y);
                 gp.AddLine(x2,y+r/2,x2-r/2,y2);
                 gp.AddLine(x+r/2,y2,x,y+r/2);
                } else {
                 gp.AddLine(x,y+r/2,x,y2-r/2);
                 gp.AddLine(x+r/2,y2,x2,y2-r/2);
                 gp.AddLine(x2,y+r/2,x+r/2,y);
                }
            } else if(ctrl&&shift) {
              if(GDI.CtrlRKey) {
                if(GDI.ShiftRKey) ControlPaint.DrawCheckBox(gr,x,y,x2-x+1,y2-y+1,ButtonState.Checked);
                else ControlPaint.DrawRadioButton(gr,x,y,x2-x+1,y2-y+1,ButtonState.Checked);
              } else if(GDI.ShiftRKey) {
                Rectangle rx=new Rectangle(x,y,x2-x+1,y2-y+1);
                gr.FillRectangle(Brushes.White,rx);
                ControlPaint.DrawBorder3D(gr,rx,Border3DStyle.Sunken);
              }
              else DrawRounded(gr,p,br,x,y,w,h,F4(ParseFA(Board.tbTLRound.Text)),Board.chTLFlat.Checked);
            } else if(shift) {
               gp=new GraphicsPath();
               if(GDI.ShiftRKey) {
                 if(w>=h) {
                   gp.AddLine(x,y,x,y2);
                   gp.AddLine(x2,y,x2,y2);
                 } else {
                   gp.AddLine(x,y,x2,y);
                   gp.AddLine(x,y2,x2,y2);
                 }
               } else if(w>=h) {
                 gp.AddArc(x,y,r,r,90,180);
                 gp.AddArc(x2-r,y,r,r,270,180);
                } else {
                 gp.AddArc(x,y,r,r,180,180);
                 gp.AddArc(x,y2-r,r,r,0,180);
                }
            } else if(ctrl) {
              if(GDI.CtrlRKey) ControlPaint.DrawButton(gr,x,y,x2-x+1,y2-y+1,ButtonState.Normal);
              else {
                if(!opaq) gr.FillEllipse(br,x,y,w-1,h-1);
                gr.DrawEllipse(p,x,y,w-1,h-1);
              }
            } else {
              if(!opaq) gr.FillRectangle(br,x+0.5f,y+0.5f,w-1,h-1);
              if(w==1||h==1) gr.DrawLine(p,x+0.5f,y+0.5f,x2+0.5f,y2+0.5f);
              else gr.DrawRectangle(p,x+0.5f,y+0.5f,w-1,h-1);
            }
            if(gp!=null) {
              gp.CloseFigure();
              if(br!=null) gr.FillPath(br,gp);
              gr.DrawPath(p,gp);
            }
          }
          map.FromBitmap(bm,x-lwidth,y-lwidth,w+2*lwidth,h+2*lwidth);
          DrawXOR();
          Repaint(Selection[0]-lwidth,Selection[1]-lwidth,Selection[2]+lwidth,Selection[3]+lwidth,true);
          DrawXOR();
        }
        static void DrawRounded(Graphics gr,Pen p,Brush b,float x,float y,float w,float h,float[] ra,bool flat) {
          float r0,r1,r2,r3;
          if(ra==null||ra.Length<1) r0=r1=r2=r3=0;
          else {
            ra=F4(ra); 
            r0=ra[0];r1=ra[1];r2=ra[2];r3=ra[3];
          }
          if(r0>0||r1>0||r2>0||r3>0) using(GraphicsPath gp=new GraphicsPath()) {
            float x2=x+w,y2=y+h;
            if(r0>0) {if(flat) gp.AddLine(x,y+r0,x+r0,y); else gp.AddArc(x,y,2*r0,2*r0,180,90);}
            gp.AddLine(x+r0,y,x2-r1,y);
            if(r1>0) {if(flat) gp.AddLine(x2-r1,y,x2,y+r1); else gp.AddArc(x2-2*r1,y,2*r1,2*r1,270,90);}
            gp.AddLine(x2,y+r1,x2,y2-r2);
            if(r2>0) {if(flat) gp.AddLine(x2,y2-r2,x2-r2,y2); else gp.AddArc(x2-2*r2,y2-2*r2,2*r2,2*r2,0,90);}
            gp.AddLine(x2-r2,y2,x+r3,y2);
            if(r3>0) {if(flat) gp.AddLine(x+r3,y2,x,y2-r3); else gp.AddArc(x,y2-2*r3,2*r3,2*r3,90,90);}
            gp.AddLine(x,y2-r3,x,y+r0);
            gp.CloseFigure();
            if(b!=null&&b!=Brushes.Transparent) gr.FillPath(b,gp);
            if(p!=null) gr.DrawPath(p,gp);
          } else {
            if(b!=null) gr.FillRectangle(b,x,y,w,h);
            if(p!=null) gr.DrawRectangle(p,x,y,w,h);
          }
        }
        static void DrawDiamond(Graphics gr,Pen p,Brush b,float x,float y,float w,float h) {
          using(GraphicsPath gp=new GraphicsPath()) {
            gp.AddLine(x+w/2,y,x+w,y+h/2);
            gp.AddLine(x+w/2,y+h,x,y+h/2);
            gp.CloseFigure();
            if(b!=null&&b!=Brushes.Transparent) gr.FillPath(b,gp);
            if(p!=null) gr.DrawPath(p,gp);
          }
        }
        static void DrawEllipse(Graphics gr,Pen p,Brush b,float x,float y,float w,float h) {
          using(GraphicsPath gp=new GraphicsPath()) {
            gp.AddEllipse(x,y,w,h);
            if(b!=null&&b!=Brushes.Transparent) gr.FillPath(b,gp);
            if(p!=null) gr.DrawPath(p,gp);
          }
        }
        void SetDash(Pen p) {
          if(p==null||Board.tbTlDash2.Value<1) return;
          float[] dash=new float[] {(float)Board.tbTlDash2.Value,(float)Board.tbTlDash2.Value};
          p.DashPattern=dash;
        }
        static float Sqr(float x,float y) { return x*x+y*y;}
        static float Dia(float x,float y) { if(x<0) x=-x;if(y<0) y=-y;return x+y;}
        void DrawText(int halign,int valign,int x,int y) {          
          string[] line=Board.tbText.Text.Replace("\r\n","\n").Split('\n');
          
          Font f=Board.GetFont(0);
          int malign=Board.taLeft.Checked?-1:Board.taRight.Checked?1:0;
          Brush bbr=Board.chTOpaq.Checked?null:new SolidBrush(IntColor(Color2));
          Color tcolor=Board.chTBWAuto.Checked?IntColor(Palette.TextColor(bbr==null?map.XY(x,y):Color2)):Board.btColor.BackColor;
          Brush tbr=new SolidBrush(tcolor);
          float vspace=0;
          int lwidth;lwidth=(int)Board.tbTLWidth2.Value;
          Pen p=lwidth<1?null:new Pen(Board.chLColorFont.Checked?tcolor:Board.blColor.BackColor,lwidth);
          SetDash(p);
          float[] round=F4(ParseFA(Board.tbTLRound.Text));
          float rm=round[0]>round[1]?round[0]:round[1];
          bool flat=Board.chTLFlat.Checked,header=line.Length>1&&Board.chTLHeader.Checked;
          int hrh=header?lwidth<0?-lwidth:lwidth>0?lwidth:1:0;          
          bool circ=Board.tsCircle.Checked,diam=Board.tsDiamond.Checked;
          int pad=(int)Board.tPadding.Value;
          if(circ||diam) malign=0;
          using(Graphics gr=Graphics.FromImage(bm)) {
            SizeF[] s=new SizeF[line.Length];
            SizeF max=new SizeF();
            for(int i=0;i<line.Length;i++) {
              s[i]=gr.MeasureString(line[i],f);
              if(s[i].Width>max.Width) max.Width=s[i].Width;
              //if(s[i].Height>max.Height) max.Height=s[i].Height;
              max.Height+=s[i].Height+vspace;
            }
            if(max.Width==0||max.Height==0) return;
            float rr,r03=0,r12=0,ew,eh,fy;
            if((rr=round[0]+round[3])>max.Height) { round[0]*=max.Height/rr;round[3]*=max.Height/rr;}
            if((rr=round[1]+round[2])>max.Height) { round[1]*=max.Height/rr;round[2]*=max.Height/rr;}
            if(circ||diam) {
              rr=0;fy=0;
              for(int ff=0;ff<max.Height;ff++) {
                float yt=-(ff+max.Height)/2,rr2=0;
                for(int i=0;i<line.Length;i++) {
                  float ym=yt+s[i].Height/2<0?yt:yt+s[i].Height;
                  float r2=circ?Sqr(s[i].Width/2,ym):Dia(s[i].Width/2,ym);
                  if(r2>rr2) rr2=r2;
                  yt+=s[i].Height+vspace;
                  if(i==0) yt+=hrh;
                }
                if(ff==0||rr2<rr) {rr=rr2;fy=ff;}
              }
              rr=(float)(circ?Math.Sqrt(rr):rr);
              fy+=(2*rr-fy-max.Height)/2;
              fy+=pad;
              //fy=pad+(rr-max.Height/2);
              ew=2*rr-max.Width;eh=2*rr-max.Height;
            } else {
              r03=Math.Max(round[0],round[3]);r12=Math.Max(round[1],round[2]);
              ew=r03+r12;eh=0;fy=pad+(lwidth>0?lwidth/2:0);
            }
            if(lwidth>0) {ew+=lwidth;eh+=lwidth;}
            ew+=2*pad;eh+=2*pad+hrh;
            Bitmap tbm=new Bitmap((int)Math.Ceiling(max.Width+ew),(int)Math.Ceiling(max.Height+eh),PixelFormat.Format32bppArgb);
            using(Graphics tg=Graphics.FromImage(tbm)) {
              //tg.FillRectangle(new SolidBrush(IntColor(Color2)),0,0,tbm.Width,tbm.Height);
              //tg.FillRectangle(new SolidBrush(Color.Transparent),0,0,tbm.Width,tbm.Height);
              if(bbr==null||r03>0||r12>0) tg.Clear(Color.Empty);
              if(circ)
                DrawEllipse(tg,p,bbr,0,0,tbm.Width-1,tbm.Height-1);
              else if(diam)
                DrawDiamond(tg,p,bbr,0,0,tbm.Width-1,tbm.Height-1);
              else
                DrawRounded(tg,p,bbr,lwidth/2f,lwidth/2f,r03+max.Width+r12+2*pad,max.Height+2*pad,round,flat);
              
              if(bbr==null) tg.TextRenderingHint=System.Drawing.Text.TextRenderingHint.AntiAlias;
              float fx=circ||diam?ew/2:lwidth/2f+r03+pad;
              for(int i=0;i<line.Length;i++) {
                float tx=malign==0?(max.Width-s[i].Width)/2:malign>0?max.Width-s[i].Width:0;              
                tg.DrawString(line[i],f,tbr,fx+tx,fy);
                fy+=s[i].Height;
                if(i==0&&hrh>0) {
                  int b=0,e=tbm.Width-1,iy=(int)fy;
                  if(p!=null||bbr!=null) {
                    while(b<e&&tbm.GetPixel(b,iy).A==0) b++;
                    while(b<e&&tbm.GetPixel(e,iy).A==0) e--;
                  }
                  Pen ph=p==null?new Pen(Board.blColor.BackColor,hrh):p;
                  ph.SetLineCap(LineCap.Round,LineCap.Round,DashCap.Round);
                  if(e-b-lwidth>0) tg.DrawLine(ph,b+lwidth/2,fy+hrh/2,e-lwidth/2,fy+hrh/2);
                  fy+=hrh;
                }
                fy+=vspace;
              }
            }
            if(!IsSelectionMode()) SetMouseOp(MouseButtons.Left,MouseOp.Select);
            PushUndo();
            DrawSelection();
            x-=halign==0?tbm.Width/2:halign>0?tbm.Width:0;
            y-=valign==0?tbm.Height/2:valign>0?tbm.Height:0;
            Selection[0]=x;Selection[1]=y;
            Selection[2]=x+tbm.Width-1;Selection[3]=y+tbm.Height-1;
            MovePaste=true;MoveBits=tbm;
            MoveTrColor=-1;MoveXor=false;
            map.CopyBitmap(MoveBits,Selection[0]+1,Selection[1]+1,MoveTrColor,Board.pasteTRX.Checked,Board.GetDiff(),Board.GetMix(),Board.GetPasteFilter());            
            Repaint(Selection[0],Selection[1],Selection[2],Selection[3],true);
            DrawSelection();
          }
          
        }


    }
    
    public static class GDI {
      public static bool CtrlRKey {get{ return 0!=(0x8000&GDI.GetKeyState(Keys.RControlKey));}}
      public static bool CtrlLKey {get{ return 0!=(0x8000&GDI.GetKeyState(Keys.LControlKey));}}
      public static bool CtrlKey {get{ return 0!=(0x8000&GDI.GetKeyState(Keys.ControlKey));}}
      public static bool ShiftKey {get{ return 0!=(0x8000&GDI.GetKeyState(Keys.ShiftKey));}}
      public static bool ShiftRKey {get{ return 0!=(0x8000&GDI.GetKeyState(Keys.RShiftKey));}}
      public static bool ShiftLKey {get{ return 0!=(0x8000&GDI.GetKeyState(Keys.LShiftKey));}}
      public static bool AltKey {get{ return 0!=(0x8000&GDI.GetKeyState(Keys.Menu));}}
      public static bool AltRKey {get{ return 0!=(0x8000&GDI.GetKeyState(Keys.RMenu));}}
      public static bool AltLKey {get{ return 0!=(0x8000&GDI.GetKeyState(Keys.LMenu));}}
      public static bool CapsLock {get{ return 0!=(0x0001&GDI.GetKeyState(Keys.CapsLock));}}
      public static bool ScrollLock {get{ return 0!=(0x0001&GDI.GetKeyState(Keys.Scroll));}}
      public static bool NumLock {get{ return 0!=(0x0001&GDI.GetKeyState(Keys.NumLock));}}
      public static bool LButton {get{ return 0!=(0x8000&GDI.GetKeyState(Keys.LButton));}}
      public static bool RButton {get{ return 0!=(0x8000&GDI.GetKeyState(Keys.RButton));}}
    
     [DllImport("user32"), SuppressUnmanagedCodeSecurity, PreserveSig]
     public static extern short GetKeyState(Keys key);

     [DllImport("user32.dll")]
     public static extern int MapVirtualKeyEx(int uCode, int uMapType, IntPtr dwhkl);

     [DllImport("user32.dll")]
     public static extern IntPtr LoadKeyboardLayout(string pwszKLID, int Flags);


     [DllImport("gdi32.dll"),SuppressUnmanagedCodeSecurity,PreserveSig]
     public extern static int SetROP2(IntPtr hdc,int fnDrawMode);
     
     [DllImport("gdi32.dll"),SuppressUnmanagedCodeSecurity,PreserveSig]
     public extern static bool MoveToEx(IntPtr hdc, int x, int y, IntPtr lpPoint);

     [DllImport("gdi32.dll"),SuppressUnmanagedCodeSecurity,PreserveSig]
     public extern static bool LineTo(IntPtr hdc, int x, int y);     

     [DllImport("gdi32.dll"),SuppressUnmanagedCodeSecurity,PreserveSig]
     public static extern IntPtr SelectObject([In] IntPtr hdc,[In] IntPtr hgdiobj);

     [DllImport("gdi32.dll"),SuppressUnmanagedCodeSecurity,PreserveSig]
     public static extern IntPtr GetStockObject(int fnObject);
    
     public const int WHITE_PEN=7;
     public const int BLACK_PEN=7;
     public const int R2_XORPEN=7;
     public const int R2_NOTXORPEN=10;
     public const int R2_COPYPEN=13;

     [DllImport("user32.dll",SetLastError=true),SuppressUnmanagedCodeSecurity,PreserveSig]
     [return: MarshalAs(UnmanagedType.Bool)]
     public static extern bool GetCursorPos(out POINT lpPoint);
     [DllImport("user32.dll"),SuppressUnmanagedCodeSecurity,PreserveSig]
     [return: MarshalAs(UnmanagedType.Bool)]
     public static extern bool SetCursorPos(int x, int y);
     
     public static bool MoveCursor(int dx,int dy,out POINT xy) {
       if(!GetCursorPos(out xy)) return false;
       if(!SetCursorPos(xy.x+=dx,xy.y+=dy)) return false;
       return true;       
     }
     
     
     [StructLayout(LayoutKind.Sequential)]
     public struct POINT {
       public int x,y;
     }
    }


		public static class X {
		  public static int t(string text,int def) {
			  int i;
			  return int.TryParse(text,out i)?i:def;
			}			
			public static double t(string text,double def) {
			  double d;
			  return double.TryParse(text.Replace(",","."),System.Globalization.NumberStyles.Float,System.Globalization.CultureInfo.InvariantCulture,out d)?d:def;
			}

		}


}
