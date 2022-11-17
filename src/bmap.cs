using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;


namespace fill {
    public struct fillres {
      public int x0,y0,x1,y1,m;
      public void Add(int x,int y) {
        if(m==0) { 
          x0=x1=x;y0=y1=y;
        } else {
          if(x<x0) x0=x;else if(x>x1) x1=x;
          if(y<y0) y0=y;else if(y>y1) y1=y;              
        }
        m++;
      }
    }
    public class Shape {
      public int[] pts;
      public bool[] move;
      
      public Shape(int len) { pts=new int[len];}      
      public Shape(Shape src) { 
        if(src.pts!=null) pts=src.pts.Clone() as int[];
        if(src.move!=null) move=src.move.Clone() as bool[];
      }
      public static int[] BoundingBox(int[] pts) {
        if(pts==null||pts.Length<2) return null;
        int[] bb=new int[4];
        bb[0]=bb[2]=pts[0];bb[1]=bb[3]=pts[1];
        for(int i=2;i<pts.Length;i++) {
          if(pts[i]<bb[0]) bb[0]=pts[i];else if(pts[i]>bb[2]) bb[2]=pts[i];
          i++;
          if(pts[i]<bb[1]) bb[1]=pts[i];else if(pts[i]>bb[3]) bb[3]=pts[i];
        }
        return bb;
      }
      public static void Move(int[] pts,int dx,int dy) {
        if(pts==null) return;
        for(int i=0;i<pts.Length;i++)
          pts[i]+=0==(i&1)?dx:dy;
      }
    }    

    public class bmap {
      public int Width,Height;
      public int[] Data; //r,g,b,h
      public const int White=0xffffff,Black=0;
      public static readonly int[] sqrt=new int[1048577];      

      public override string ToString() {
        return ""+Width+"x"+Height;
      }
      static bmap() {
        int j=0,j2=0;
        for(int i=0;i<sqrt.Length;i++) {
          if(j2<=i) {j2+=2*j+1;j++;}
          sqrt[i]=j-1;
        }
      }
      public static int isqrt(int x) {
        if(x<1) return 0;
        else if(x<sqrt.Length) return sqrt[x];
        else return (int)Math.Sqrt(x);
      }
      public static int isqrt(int x,int y) {
        return isqrt(x*x+y*y);
      }
      public static int sqr(int x) { return x*x;}
      public static int sqr(int x,int y) { return x*x+y*y;}
      public static int sabs(int x,int y) { return Math.Abs(x)+Math.Abs(y);}      
      public static int abs(int x) { return Math.Abs(x);}
      public int isqrt2(int x) {
        uint a,a2;
        if(x<4) return x>0?1:0;
        a=(uint)(x>>(bitscan(x)>>1));
        do {
          a2=a;
          a=(uint)(a2+x/a2)>>1;
        } while(a<a2);
        return (int)a2;
      }
      public static int idiv(int x,int div) {
        if(div<0) {x=-x;div=-div;}
        if(x>=0) return x/div;
        int r=x/div;
        return x==r*div?r:r-1;
      }
      public static int ceil(int x,int div) {
        if(div<2) return x;
        int r=x%div;
        return r==0?x:x-r+div;
      }
      public static int floor(int x,int div) {
        return div>1?x-x%div:x;
      }
      public static int modp(int x,int div) {
        if(div<2) return 0;
        x=x%div;
        if(x<0) x+=div;
        return x;
      }
      public static int round(int x,int div) {
        if(div<2) return 0;
        int r=x%div;
        if(2*r<div) x-=r;else x+=div-r;
        return x;
      }       

      
      public bmap() {}
      public bmap(int width,int height) {Alloc(width,height);}
      public bmap(bmap src,int x,int y,int x2,int y2,int extent) {
        int w=x2-x+1+2*extent,h=y2-y+1+2*extent;
        Alloc(w,h);
        if(src!=null) CopyRectangle(src,x,y,x2,y2,extent,extent,-1);
      }
      public bmap(string brush) {
        string[] line=brush.Split('.');
        Width=line[0].Length;Height=line.Length;
        Alloc(Width,Height);
        for(int y=0;y<Height;y++) {
          string l=line[y];
          for(int x=0;x<l.Length;x++)
            Data[y*Width+x]=l[x]!='0'?White:0;
        }
      }      
      public bmap(bmap src) {Copy(src);}
      public bmap Clone() { return new bmap(this);}
      public void Copy(bmap src) {
        if(src==null) return;
        Data=src.Data.Clone() as int[];
        Width=src.Width;Height=src.Height;
      }
      public void Alloc(int width,int height) {        
        Data=new int[(Width=width)*(Height=height)];
      }
      public int XY(int x,int y) {
        if(x<0||y<0||x>=Width||y>=Height) return -1;
        return Data[y*Width+x];
      }
      public void XY(int x,int y,int color) {
        if(x<0||y<0||x>=Width||y>=Height) return;
        Data[y*Width+x]=color;
      }
      public void XY(int x,int y,int color,bool whiteonly) {
        if(x<0||y<0||x>=Width||y>=Height) return;
        if(!whiteonly||(Data[y*Width+x]&0xffffff)==White)
          Data[y*Width+x]=color;
      }
      public bool IsSolid(int x,int y,int w,int h) {        
        if(w<1||h<1) return false;
        if(w==1&&h==1) return true;
        int p=Width*y+x,p2,c=Data[p]&White;
        for(x=0,p2=p+(h-1)*Width;x<w;x++)
          if(c!=(Data[p+x]&White)||c!=(Data[p2+x]&White)) return false;
        for(y=1,p2=p+w-1;y<h-1;y++)
          if(c!=(Data[p+y*Width]&White)||c!=(Data[p2+y*Width]&White)) return false;
        for(y=1;y<h-1;y++)
          for(x=1,p2=p+y*Width+1;x<w-1;x++,p2++)
            if(c!=(Data[p2]&White)) return false;
        return true;
      }
      
      public void Brush(int x,int y,int color,bmap brush,bool whiteonly) {
        if(brush==null) { XY(x,y,color,whiteonly);return;}
        int bx=brush.Width/2,by=brush.Height/2;
        if(x<-brush.Width||y<-brush.Height||x>=Width+brush.Width||y>=Height+brush.Height) return;
        for(int i=0;i<brush.Height;i++) {
          int dy=y+i-by;
          if(dy<0||dy>=Height) continue;
          for(int j=0;j<brush.Width;j++) {
           int dx=x+j-bx;
           if(dx<0||dx>=Width) continue;
           if(brush.Data[i*brush.Width+j]!=0) 
             if(!whiteonly||(Data[dy*Width+dx]&0xffffff)==White) Data[dy*Width+dx]=color;
          }
        }
      }

      public void LeaveBlack() {
        for(int i=0;i<Data.Length;i++) {
          int x=Data[i]&White;
          if(x!=0&&x!=White) Data[i]=White;
        }
      }      
      public void FillRectangle(int x,int y,int x2,int y2,int color) { FillRectangle(x,y,x2,y2,color,0);}
      public void FillRectangle(int x,int y,int x2,int y2,int color,int cmode) {
        int r;
        if(x2<x) {r=x;x=x2;x2=r;}
        if(y2<y) {r=y;y=y2;y2=r;}
        if(x2<0||y2<0||x>=Width||y>=Height) return;
        if(x<0) x=0;if(x2>=Width) x2=Width-1;
        if(y<0) y=0;if(y2>=Height) y2=Height-1;
        int h=y*Width+x,n=x2-x+1;
        while(y<=y2) {
          if(cmode>0)
            for(int he=h+n;h<he;h++)
              Data[h]=Palette.Colorize(cmode,color,Data[h]);
          else
            for(int he=h+n;h<he;h++) Data[h]=color;
          h+=Width-n;
          y++;
        }        
      }
			public void SmartSelect(int[] rect) {
			  R.Norm(rect);
				R.Intersect(rect,0,0,Width-1,Height-1);			
				if(rect[0]==rect[2]||rect[1]==rect[3]) return;
				int c=White&(Data[rect[1]*Width+rect[0]]),x,y;
				while(rect[0]<rect[2]) {
					for(y=rect[1];y<=rect[3];y++)
					  if((Data[y*Width+rect[0]]&White)!=c) break;
				  if(y>rect[3]) rect[0]++;else break;
				}
				while(rect[0]<rect[2]) {
					for(y=rect[1];y<=rect[3];y++)
					  if((Data[y*Width+rect[2]]&White)!=c) break;
				  if(y>rect[3]) rect[2]--;else break;
				}
				while(rect[1]<rect[3]) {
					for(x=rect[0];x<=rect[2];x++)
					  if((Data[rect[1]*Width+x]&White)!=c) break;
				  if(x>rect[2]) rect[1]++;else break;
				}
				while(rect[1]<rect[3]) {
					for(x=rect[0];x<=rect[2];x++)
					  if((Data[rect[3]*Width+x]&White)!=c) break;
				  if(x>rect[2]) rect[3]--;else break;
				}
			}
      public void Erase(int x,int y,int x2,int y2,bool dox,bool doy) { Erase(x,y,x2,y2,dox?doy?0:1:doy?2:0);}
			public void Erase(int x,int y,int x2,int y2,int mode) {
        R.Norm(ref x,ref y,ref x2,ref y2);
        if(!IntersectRect(ref x,ref y,ref x2,ref y2,0,0,Width-1,Height-1)) return;
        if(x2<0||y2<0||x>=Width||y>=Height) return;
				int width=x2-x+1,height=y2-y+1;
        int y0=y<1?0:y-1,y1=y2+1<Height?y2+1:y2;
        int x0=x<1?0:x-1,x1=x2+1<Width?x2+1:x2;
        int dx=2*(x2-x+1),dy=2*(y2-y+1);
        for(int iy=y;iy<=y2;iy++) {
          int cy=Data[iy*Width+x0],cy2=Data[iy*Width+x1];
          for(int ix=x;ix<=x2;ix++) {
					  int c;						
					  if(mode>=3) {
							int _x=ix-x,_y=iy-y,w1=width+1,h1=height+1,_x2=(_y+1)*w1,_y2=(_x+1)*h1,cx2;
							int xa,ya,xb,yb,xc,yc,xd,yd,ca,cb,cc,cd,ab;
							if((width-_x)*height>=width*(_y+1)) {
								xa=(h1*(_x+1)+_x2)/h1;ya=0;xb=0;yb=(w1*(_y+1)+_y2)/w1;ab=xa+yb<1?128:(_x+1+yb-_y)*255/(xa+yb);
							} else {
								xa=w1;ya=(w1*(_y+1-h1)+_y2)/w1;xb=(h1*(_x+1-w1)+_x2)/h1;yb=h1;ab=width-xb+height-ya<1?128:(_x+1-xb+height-_y)*255/(width-xb+height-ya);
							}
							if(xa<0) xa=0;else if(xa>w1) xa=w1;
							if(xb<0) xb=0;else if(xb>w1) xb=w1;
							if(ya<0) ya=0;else if(ya>h1) ya=h1;
							if(yb<0) yb=0;else if(yb>h1) yb=h1;
							ca=Data[(y+ya-1)*Width+x+xa-1];cb=Data[(y+yb-1)*Width+x+xb-1];
							c=Palette.RGBMix(cb,ca,ab,255);

							if((_x+1)*height<width*(_y+1)) {
								xc=0;yc=(w1*(_y+1)-_y2)/w1;xd=(h1*(_x+1+w1)-_x2)/h1;yd=h1;ab=xd+height-yc<1?128:(_x+1+_y-yc)*255/(xd+height-yc);
							} else {
								xc=(h1*(_x+1)-_x2)/h1;yc=0;xd=w1;yd=(w1*(_y+1+h1)-_y2)/w1;ab=width-xc+yd<1?128:(_x-xc+_y+1)*255/(width-xc+yd);
							}
							if(xc<0) xa=0;else if(xa>w1) xa=w1;
							if(xd<0) xd=0;else if(xd>w1) xd=w1;
							if(yc<0) yc=0;else if(yc>h1) yc=h1;
							if(yd<0) yd=0;else if(yd>h1) yd=h1;
							cc=Data[(y+yc-1)*Width+x+xc-1];cd=Data[(y+yd-1)*Width+x+xd-1];
							cx2=Palette.RGBMix(cc,cd,ab,255);
							if(mode!=3) c=mode==4?cx2:Palette.RGBMix(c,cx2,128,255);
						} else {
						  bool dox=mode<2,doy=mode!=1;
              int cx=Data[y0*Width+ix],cx2=Data[y1*Width+ix];
              cx=doy?Palette.RGBMix(cx,cx2,2*(iy-y)+1,dy):0;
              cx2=dox?Palette.RGBMix(cy,cy2,2*(ix-x)+1,dx):0;
							c=Palette.RGBMix(cx,cx2,1-(dox?0:1)+(doy?0:1),2);
						}
            Data[iy*Width+ix]=c;
          }
        }        
      }
      public void EraseColor(int x,int y,int x2,int y2,int color,bool vert,bool hori) {
        R.Norm(ref x,ref y,ref x2,ref y2);
        if(!IntersectRect(ref x,ref y,ref x2,ref y2,1,1,Width-2,Height-2)) return;
        ClearByte(x,y,x2,y2,true,color);        
        RectByte(x-1,y-1,x2+1,y2+1,1);
        int c24=1<<24;
        if(vert) {
          for(int x3=x;x3<=x2;x3++) {
            for(int p=y*Width+x3,pe=p+(y2-y)*Width;p<pe;p+=Width)
              if(0==(Data[p]&c24)) {
                int c1=Data[p-Width],p2=p,n=2;
                while(0==(Data[p+=Width]&c24)) n++;
                int c2=Data[p],i=1;
                while(p2<p) {
                  Data[p2]=Palette.RGBMix(c1,c2,i++,n);
                  p2+=Width;
                }
              }
          }
        }
        if(hori) {
          for(int y3=y;y3<=y2;y3++) {
            for(int p=y3*Width+x,pe=p+x2-x;p<pe;p++)
              if(0==(Data[p]&c24)) {
                int c1=Data[p-1],p2=p,n=2;
                while(0==(Data[p+=1]&c24)) n++;
                int c2=Data[p],i=1;
                while(p2<p) {
                  int c=Palette.RGBMix(c1,c2,i++,n);
                  if(vert) c=Palette.RGBMix(c,Data[p2],1,2);
                  Data[p2]=c;
                  p2++;
                }
              }
          }
        }
        ClearByte(x-1,y-1,x2+1,y2+1,0);
      }
			public void BW(int x,int y,int x2,int y2,int minmax,int blacklevel) {
        R.Norm(ref x,ref y,ref x2,ref y2);
        if(!IntersectRect(ref x,ref y,ref x2,ref y2,0,0,Width-1,Height-1)) return;
        if(x2<0||y2<0||x>=Width||y>=Height) return;
        for(int iy=y;iy<=y2;iy++) 
          for(int ix=x;ix<=x2;ix++) {
					  int c=Data[iy*Width+ix];
						int r=c&255,g=(c>>8)&255,b=(c>>16)&255;
						if(minmax<0) r=r<g?r<b?r:b:g<b?g:b;
            else if(minmax==2) r=r*r+g*g+b*b<blacklevel*blacklevel?0:255;
            else if(minmax==3) r=(r+3*g+2*b+1)/6;
						else if(minmax>0) r=r>g?r>b?r:b:g>b?g:b;
						else r=r+g+b;
						Data[iy*Width+ix]=r<blacklevel?0:White;
					}			  
			}
			public void C8(int x,int y,int x2,int y2,int level) {
        R.Norm(ref x,ref y,ref x2,ref y2);
        if(!IntersectRect(ref x,ref y,ref x2,ref y2,0,0,Width-1,Height-1)) return;
        if(x2<0||y2<0||x>=Width||y>=Height) return;
        for(int iy=y;iy<=y2;iy++) 
          for(int ix=x;ix<=x2;ix++) {
					  int c=Data[iy*Width+ix];
						int r=c&255,g=(c>>8)&255,b=(c>>16)&255;
            r=r<level?0:255;
            g=g<level?0:255;
            b=b<level?0:255;
						Data[iy*Width+ix]=r|(g<<8)|(b<<16);
					}			  
			}
			public void Gray(int x,int y,int x2,int y2,int minmax) {
        R.Norm(ref x,ref y,ref x2,ref y2);
        if(!IntersectRect(ref x,ref y,ref x2,ref y2,0,0,Width-1,Height-1)) return;
        if(x2<0||y2<0||x>=Width||y>=Height) return;
        for(int iy=y;iy<=y2;iy++) 
          for(int ix=x;ix<=x2;ix++) {
					  int c=Data[iy*Width+ix];
						int r=c&255,g=(c>>8)&255,b=(c>>16)&255;
						if(minmax<0) r=r<g?r<b?r:b:g<b?g:b;
            else if(minmax==2) r=isqrt(r*r+g*g+b*b)*255/441;
            else if(minmax==3) r=(r+3*g+2*b+1)/6;
						else if(minmax>0) r=r>g?r>b?r:b:g>b?g:b;
						else r=(r+g+b)/3;
						Data[iy*Width+ix]=r|(r<<8)|(r<<16);
					}			  
			}
      public void MoveRectangle(int x,int y,int x2,int y2,int dx,int dy,int bgcolor,int trcolor,bool cs) {
        int w=x2-x+1,h=y2-y+1;
        bmap bm2=new bmap(w,h);
        bm2.Clear(bgcolor);
        bm2.CopyRectangle(this,x,y,x2,y2,0,0,-1);
        if(bgcolor!=-1) FillRectangle(x,y,x2,y2,bgcolor);
        if(cs) {
          if(x>dx&&y==dy) CopyRectangle(this,dx,y,x-1,y2,x2+dx-x+1,y,-1);
          if(x<dx&&y==dy) CopyRectangle(this,x2+1,y,x2+dx-x,y2,x,y,-1);
          if(dx==x&&y>dy) CopyRectangle(this,x,dy,x2,y-1,x,y2+dy-y+1,-1);
          if(dx==x&&y<dy) CopyRectangle(this,x,y2+1,x2,y2+dy-y,x,y,-1);
        }
        CopyRectangle(bm2,0,0,w-1,h-1,dx,dy,trcolor);
      }
      public void MoveRectangle(int x,int y,int x2,int y2,int dx,int dy,bmap back) {
        int a=x,b=y;
        IntersectRect(ref x,ref y,ref x2,ref y2,0,0,Width-1,Height-1);
        if(x>=Width||y>=Height||x2<0||y2<0) return;
        dx+=x-a;dy+=y-b;
        int dx2=dx+x2-x,dy2=dy+y2-y;
        a=dx;b=dy;
        IntersectRect(ref dx,ref dy,ref dx2,ref dy2,0,0,Width-1,Height-1);
        if(dx>=Width||dy>=Height||dx2<0||dy2<0) return;
        x+=dx-a;y+=dy-b;x2=x+dx2-dx;y2=y+dy2-dy;
        
        int w=x2-x+1,h=y2-y+1;
        bmap bm2=new bmap(w,h);
        bm2.CopyRectangle(this,x,y,x2,y2,0,0,-1);
        if(back!=null) CopyRectangle(back,x,y,x2,y2,x,y,-1);
        CopyRectangle(bm2,0,0,w-1,h-1,dx,dy,-1);
      }      
      
      public void CopyRectangle(bmap src,int x,int y,int x2,int y2,int dx,int dy,int trcolor) {
        R.Norm(ref x,ref y,ref x2,ref y2);
        int tx=dx-x,ty=dy-y;
        if(!R.Intersect(ref x,ref y,ref x2,ref y2,0,0,src.Width-1,src.Height-1)) return;
        if(!R.Intersect(ref x,ref y,ref x2,ref y2,-tx,-ty,-tx+Width-1,-ty+Height-1)) return;        
        dx=x+tx;dy=y+ty;
        int n=x2-x+1,g=dy*Width+dx,h=y*src.Width+x;
        while(y<=y2) {
          for(int he=h+n;h<he;h++,g++) {
            int c=src.Data[h];
            if(c!=trcolor) Data[g]=c;
          }
          g+=Width-n;h+=src.Width-n;
          y++;
        }
      }
      public delegate int DelegateSearch(object param,bmap map,int x,int y,bmap search);
      public fillres SearchRectangle(bmap src,bmap search,int sx,int sy,int w,int h,bool over,int color,int trcolor,int cmode,DelegateSearch OnReplace,object param) {
        search.ClearByte();
        color&=White;
        int c00,c10,c01,c11,o00,o10,o01,o11;
        bool[] mask=null;
        fillres fr=new fillres();
        if(trcolor==-1) {
          o00=0;o10=w-1;o01=Width*(h-1);o11=o10+o01;
          c00=search.XY(sx,sy);c10=search.XY(sx+w-1,sy);c01=search.XY(sx,sy+h-1);c11=search.XY(sx+w-1,sy+h-1);
        } else {
          trcolor&=White;
          mask=new bool[w*h];
          bool f1,f2,f3,f4=f3=f2=f1=false;
          c00=c10=c01=c11=o00=o01=o10=o11=-1;
          for(int j=0;j<h;j++) for(int i=0;i<w;i++) mask[w*j+i]=search.XY(i,j)!=trcolor;
          for(int i=0;i<w*h;i++) {
            int a=i%w,b=i/w;
            if(!f1&&mask[b*w+a]) { c00=search.XY(a,b);o00=Width*b+a;f1=true;}
            a=w-a-1;b=h-b-1;
            if(!f2&&mask[b*w+a]) { c11=search.XY(a,b);o11=Width*b+a;f2=true;}
            a=i/h;b=h-i%h-1;
            if(!f3&&mask[b*w+a]) { c01=search.XY(a,b);o01=Width*b+a;f3=true;}
            a=w-a-1;b=h-b-1;
            if(!f4&&mask[b*w+a]) { c10=search.XY(a,b);o10=Width*b+a;f4=true;}
          }
          if(!(f1&&f2&&f3&&f4)) return fr;
        }
        c00&=White;c01&=White;c10&=White;c11&=White;
        ClearByte();        
        int[] data=src.Data,sdata=search.Data;
        for(int y=1;y<Height-h;y++) {
          for(int x=1,o=y*Width+1;x<Width-w;x++,o++) if(data[o+o00]==c00&&data[o+o10]==c10&&data[o+o01]==c01&&data[o+o11]==c11) {
            for(int j=0;j<h;j++)
              if(trcolor==-1) {
                for(int i=0,p=o+(j)*Width,q=(sy+j)*search.Width+sx;i<w;i++,p++,q++) if(data[p]!=sdata[q]) goto fail;
              } else {
                for(int i=0,p=o+(j)*Width,q=(sy+j)*search.Width+sx,r=j*w+i;i<w;i++,p++,q++,r++) if(mask[r]&&data[p]!=sdata[q]) goto fail;
              }
            if(fr.m==0) {fr.x0=x;fr.y0=y;fr.x1=x+w-1;fr.y1=y+h-1;}
            else R.Union(ref fr.x0,ref fr.y0,ref fr.x1,ref fr.y1,x,y,x+w-1,y+h-1);
            fr.m++;
            if(OnReplace==null||0>=OnReplace(param,this,x,y,search)) {
              if(trcolor==-1) FillRectangle(x,y,x+w-1,y+h-1,color,cmode);
              else for(int j=0;j<h;j++)
                for(int i=0,p=o+(j)*Width,r=j*w+i;i<w;i++,p++,r++) if(mask[r]) Data[p]=cmode>0?Palette.Colorize(cmode,color,Data[p]):color;
            }
            if(!over) {x+=w-1;o+=w-1;}
           fail:;
          }
        }
        return fr;
      }
      public long FindRectangle(ref int x,ref int y,int x2,int y2,bool backward,int level) {
        R.Norm(ref x,ref y,ref x2,ref y2);        
        long min=long.MaxValue;
        if(!IntersectRect(ref x,ref y,ref x2,ref y2,1,1,Width-2,Height-2)) { x=y=-1;return min;}
        ClearByte();
        int w=x2-x+1,h=y2-y+1,xi=x+w,yi=y,xf=-1,yf=-1,xd=backward?-1:1;
        long limit2=1+level*w*h;
        for(int ye=backward?0:Height-1-h;yi!=ye;) {
          bool i=yi>=y-h&&yi<=y2;
          for(int xe=backward?0:Width-1-w;xi!=xe;xi+=xd) {
            if(i&&xi>=x-w&&xi<=x2) continue;
            long d=DiffRect(x,y,x2,y2,xi,yi,limit2);
            if(d<min) {
              xf=xi;yf=yi;
              if((min=d)==0||d<limit2) goto found;
              //if(d<limit2) limit2=d;
            }
          }
          if(backward) {xi=Width-w-1;yi--;} else {xi=1;yi++;}
        }
       found:
        x=xf;y=yf;
        return min;
      }
      public static int DiffPixel(int src,int dst) {
        byte rs=(byte)(src&255),gs=(byte)(src>>8),bs=(byte)(src>>16);
        byte rd=(byte)(dst&255),gd=(byte)(dst>>8),bd=(byte)(dst>>16);
        return (rd>rs?rd-rs:rs-rd)+(gd>gs?gd-gs:gs-gd)+(bd>bs?bd-bs:bs-bd);
      }
      public long DiffRect(int x,int y,int x2,int y2,int dx,int dy,long limit) {
        long diff=0;
        int w=x2-x+1,h=y2-y+1;
        int src=y*Width+x,dst=dy*Width+dx;
        bool exact=limit==1;
        for(int yi=0;yi<h;yi++,src+=Width-w,dst+=Width-w) {          
          for(int xi=0;xi<w;xi++,src++,dst++) {
            int cs=Data[src],cd=Data[dst];
            if(exact) {
              if(cs!=cd) return long.MaxValue;
            } else
              diff+=DiffPixel(Data[src],Data[dst]);
          }            
          if(diff>=limit) return long.MaxValue;          
        }
        return exact?0:diff;
      }
      public static bool IntersectRect(ref int x,ref int y,ref int x2,ref int y2,int ix,int iy,int ax,int ay) {
        int r;
        if(x>x2) {r=x;x=x2;x2=r;};
        if(y>y2) {r=y;y=y2;y2=r;};
        if(ix>ax) {r=ix;ix=ax;ax=r;};
        if(iy>ay) {r=iy;iy=ay;ay=r;};
        if(x>ax||x2<ix||y>ay||y2<iy) return false;
        if(x<ix) x=ix;if(x2>ax) x2=ax;
        if(y<iy) y=iy;if(y2>ay) y2=ay;
        return true;        
      }
            
      public void Save(string filename) {
        Bitmap bm=new Bitmap(Width-2,Height-2,PixelFormat.Format32bppRgb);
        ToBitmap(this,bm,false);
        bm.Save(filename);
        bm.Dispose();
      }
      public void FromBitmap(Bitmap bm,int x,int y,int width,int height) {
        if(x<0) {width+=x;x=0;}
        if(y<0) {height+=y;y=0;}
        if(x+width>bm.Width) width=bm.Width-x;
        if(y+height>bm.Height) height=bm.Height-y;
        if(width<1||height<1) return;
        Rectangle r=new Rectangle(x,y,width,height);
        BitmapData bd=bm.LockBits(r,ImageLockMode.ReadOnly,PixelFormat.Format32bppRgb);
        for(int i=0;i<bd.Height;i++) {
          int addr=(1+y+i)*Width+1+x;
          Marshal.Copy(new IntPtr(bd.Scan0.ToInt64()+bd.Stride*i),Data,addr,bd.Width);
          for(int e=addr+bd.Width;addr<e;addr++) Data[addr]&=White;
        }
        bm.UnlockBits(bd);        
      }
      public static bmap FromBitmap(bmap map,Bitmap bm,int trcolor) {
        if(bm==null) return map;
        if(map==null) map=new bmap();
        map.Alloc(bm.Width+2,bm.Height+2);
        map.Clear();
        Rectangle r=new Rectangle(0,0,bm.Width,bm.Height);
        bool alpha=bm.PixelFormat==PixelFormat.Format32bppArgb;
        BitmapData bd=bm.LockBits(r,ImageLockMode.ReadOnly,alpha?PixelFormat.Format32bppArgb:PixelFormat.Format32bppRgb);
        for(int y=0;y<bd.Height;y++)
          Marshal.Copy(new IntPtr(bd.Scan0.ToInt64()+bd.Stride*y),map.Data,(1+y)*map.Width+1,bd.Width);
			  if(trcolor>=-1) map.ClearByte(alpha?trcolor<0?White:trcolor:-1);
        bm.UnlockBits(bd);
        return map;
      }
      public static bool IsAlpha(Bitmap b) { return b!=null&&0!=(b.PixelFormat&PixelFormat.Alpha);}
      public int[] CopyBitmap(Bitmap bm,int x,int y,int trcolor,bool trx,int diffmode,int mixlevel,int filter) {
        if(bm==null||x>=Width||y>=Height||x+bm.Width<=0||y+bm.Height<=0) return null;
        int[] sel=new int[] {x,y,x+bm.Width-1,y+bm.Height-1};
        if(!R.Intersect(sel,0,0,Width-1,Height-1)) return null;
        bool alpha=IsAlpha(bm),mix=mixlevel>0,xor=diffmode==1,diff=diffmode>1,fil=filter!=-1,filb=0!=(filter&0x1000000);
        Rectangle r=new Rectangle(sel[0]-x,sel[1]-y,sel[2]-sel[0]+1,sel[3]-sel[1]+1);
        BitmapData bd=bm.LockBits(r,ImageLockMode.ReadOnly,alpha?PixelFormat.Format32bppArgb:PixelFormat.Format32bppRgb);
        int g=sel[1]*Width+sel[0],n=sel[2]-sel[0]+1,stride=bd.Stride;
        long h=bd.Scan0.ToInt64();
        bool tr=trcolor!=-1;
        trx&=tr;
        int[] buf=alpha||diff||xor||tr||mix||fil?new int[n]:null,prev=trx?new int[n]:null,next=trx?new int[n]:null;
        trcolor&=White;
        if(next!=null) Marshal.Copy(new IntPtr(h),next,0,n);
        for(y=sel[1];y<=sel[3];y++) {
          if(buf==null) {
            Marshal.Copy(new IntPtr(h),Data,g,n);
          } else {
            if(prev!=null) Array.Copy(buf,prev,buf.Length);
            if(next!=null) {
              Array.Copy(next,buf,buf.Length);
              if(y<sel[3]) Marshal.Copy(new IntPtr(h+stride),next,0,n);
            } else
              Marshal.Copy(new IntPtr(h),buf,0,n);
            for(int i=0;i<n;i++) {
              int c=buf[i]&White;              
              if(!tr||c!=trcolor) {
                int c2;
                if(fil) {
                  c2=Data[g+i]&White;
                  if(filter>=0?c2!=filter:c2==(filter&White)||(filb&&c2==0)) continue;
                }
                if(alpha) {
                  int a=(buf[i]>>24)&255;
                  if(a==0) continue;
                  c=Palette.RGBMix(Data[g+i],c,a,255);
                }
                if(diff) {
                  c2=Data[g+i]&White;
                  bool eq=c==c2;
                  switch(diffmode) {
                   default:
                   case 2:c2=Palette.ColorIntensity765(c2,c==c2?511+Palette.RGBSum(c2)/3:Palette.RGBSum(Palette.RGBMix(c,c2,1,2))/3);break;
                   case 3:c2=Palette.ColorIntensity765(eq?c2:0xff0000,eq?511+Palette.RGBSum(c2)/3:Palette.RGBSum(Palette.RGBMix(c,c2,1,2))/3);break;
                   case 4:c2=Palette.ColorIntensity765(0,765-Palette.RGBSum(Palette.RGBDiff(c,c2)));break;
                   case 5:c2=Palette.RGBMin(c,c2);break;
                   case 6:c2=Palette.RGBMix(c,c2,1,2);break;
                   case 7:c2=Palette.RGBMax(c,c2);break;
                   case 8:c2=Palette.RGBEmbo(c,c2,4);break;
                   case 9:c2=Palette.RGBXmix(c,c2);break;
                   case 10:c2=White^Palette.RGBDiff(c,c2,4);break;
                   case 11:c2=White^Palette.RGBSqrt(c,c2,4);break;
                  }
                } else 
                  c2=xor?White^c^Data[g+i]:mix?Palette.RGBMix(Data[g+i],c,mixlevel,100):c;
                if(trx) {
                  int a=0,b=0,ab;
                  bool u=y>sel[1],d=y<sel[3];
                  if(u&&(prev[i]&White)==trcolor) a++;
                  if(d&&(next[i]&White)==trcolor) a++;
                  if(i>0) {
                    if((buf[i-1]&White)==trcolor) a++;
                    if(u&&(prev[i-1]&White)==trcolor) b++;
                    if(d&&(next[i-1]&White)==trcolor) b++;
                  }
                  if(i<n-1) {
                    if((buf[i+1]&White)==trcolor) a++;
                    if(u&&(prev[i+1]&White)==trcolor) b++;
                    if(d&&(next[i+1]&White)==trcolor) b++;
                  }
                  ab=(a>0?85:0)+(b>0?85:0);
                  c2=Palette.RGBMix(c,Data[g+i],ab,255);
                }
                Data[g+i]=c2&White;
              }
            }
          }
          g+=Width;h+=stride;
        }
        if(buf==null) AndOr(White,0,sel[0],sel[1],sel[2],sel[3]);
        bm.UnlockBits(bd);
        return sel;
      }
      public static void ToBitmap(bmap map,Bitmap bm,bool alpha) {
        Rectangle r=new Rectangle(0,0,bm.Width,bm.Height);                
        BitmapData bd=bm.LockBits(r,ImageLockMode.WriteOnly,alpha?PixelFormat.Format32bppArgb:PixelFormat.Format32bppRgb);
        for(int y=0;y<bd.Height;y++)
          Marshal.Copy(map.Data,(1+y)*map.Width+1,new IntPtr(bd.Scan0.ToInt64()+bd.Stride*y),bd.Width);
        bm.UnlockBits(bd);
        map.ClearByte();
      }      
      public static void ToBitmap(bmap map,int dx,int dy,Bitmap bm,int x0,int y0,int x1,int y1,bool alpha) {
        int e;
        if(x1<x0) {e=x0;x0=x1;x1=e;}
        if(y1<y0) {e=y0;y0=y1;y1=e;}
        if(x0<0) {dx-=x0;x0=0;}
        if(y0<0) {dy-=y0;y0=0;}
        if(x1>=bm.Width) x1=bm.Width-1;
        if(y1>=bm.Height) y1=bm.Height-1;
        if(dx<0) {x0-=dx;dx=0;}
        if(dy<0) {y0-=dy;dy=0;}
        int w=x1-x0+1,h=y1-y0+1;
        if(dx+w>map.Width) {w=map.Width-dx;x1=x0+w-1;}
        if(dy+h>map.Height) {h=map.Height-dy;y1=y0+h-1;}
        if(w<1||h<1||x0>=bm.Width||y0>=bm.Height||dx>=map.Width||dy>=map.Height) return;
        Rectangle r=new Rectangle(x0,y0,w,h);
        BitmapData bd=bm.LockBits(r,ImageLockMode.WriteOnly,alpha?PixelFormat.Format32bppArgb:PixelFormat.Format32bppRgb);
        for(int y=0;y<h;y++)
          Marshal.Copy(map.Data,(dy+y)*map.Width+dx,new IntPtr(bd.Scan0.ToInt64()+bd.Stride*y),w);
        bm.UnlockBits(bd);
      }
      
      public void Transparent(int color) {
        if(color<0) return;
        for(int i=0;i<Data.Length;i++) {
          int c=Data[i]&White;
          if(c==color) Data[i]&=White;
          else Data[i]|=255<<24;          
        }
      }

      //public static void ToBitmap(bmap map,Bitmap bm,int sx,int sy,int zoom,int border) {} 
      public static void Color(int c,out byte r,out byte g,out byte b) {
        r=(byte)(c&255);
        g=(byte)((c>>8)&255);
        b=(byte)((c>>16)&255);
      }
      public void Clear() { Clear(0xffffff);}
      public void Clear(int color) {
        for(int i=0;i<Data.Length;i++) Data[i]=color;
      }
      public void AndOr(int and,int or) {
        for(int i=0;i<Data.Length;i++) Data[i]=or|(and&Data[i]);
      }
      public void AndOr(int and,int or,int x0,int y0,int x1,int y1) {
        IntersectRect(ref x0,ref y0,ref x1,ref y1,0,0,Width-1,Height-1);
        while(y0<=y1) {
          int h=y0*Width+x0;
          for(int he=h+x1-x0;h<=he;h++)
            Data[h]=or|(and&Data[h]);
          y0++;
        }        
      }      
      public void ClearByte() {
        for(int i=0;i<Data.Length;i++)
          Data[i]&=0xffffff;
      }
      public void ClearByte(int color) {
        if(color<0) { ClearByte();return;}
        int b0=color&255,b1=(color>>8)&255,b2=(color>>16)&255;
        for(int i=0;i<Data.Length;i++) {
          int a=(Data[i]>>24)&255;
          Data[i]&=0xffffff;          
          if(a<255) {
            int x=Data[i],b=255-a;
            int c0=(b*b0+a*(x&255))/255;
            int c1=(b*b1+a*((x>>8)&255))/255;
            int c2=(b*b2+a*((x>>16)&255))/255;
            Data[i]=c0|(c1<<8)|(c2<<16);
          }
        }
      }
      public void ClearByte(int x0,int y0,int x1,int y1,byte value) {
       for(int y=y0;y<=y1;y++)
         for(int x=x0,p=y*Width+x;x<=x1;x++,p++) 
           Data[p]=(value<<24)|(White&Data[p]);
      }
      public void ClearByte(int x0,int y0,int x1,int y1,bool neq,int color) {
       color&=White;
       for(int y=y0;y<=y1;y++)
         for(int x=x0,p=y*Width+x;x<=x1;x++,p++) {
           int c=Data[p]&White;
           byte b=(byte)((c==color)^neq?1:0);
           Data[p]=(b<<24)|c;
         }
      }
      public void RectByte(int x0,int y0,int x1,int y1,byte value) {
       for(int x=x0,p0=y0*Width+x,p1=y1*Width+x;x<=x1;x++,p0++,p1++) {
         Data[p0]=(value<<24)|(White&Data[p0]);
         Data[p1]=(value<<24)|(White&Data[p1]);
       }
       for(int y=y0,p0=y0*Width+x0,p1=y0*Width+x1;y<=y1;y++,p0+=Width,p1+=Width) {
         Data[p0]=(value<<24)|(White&Data[p0]);
         Data[p1]=(value<<24)|(White&Data[p1]);
       }
      }
      public void Border(int color) {
        for(int x=0;x<Width;x++)
          Data[x]=Data[Data.Length-1-x]=color;
        int h=0,g=Width-1;
        for(int y=0;y<Height;y++) {
          Data[h]=Data[g]=color;
          h+=Width;g+=Width;
        }
      }
      public void Border() {
        for(int x=0,x2=(Height-1)*Width;x<Width;x++,x2++) {
          Data[x]=Data[x+Width];
          Data[x2]=Data[x2-Width];
        }
        for(int y=0,h=0,g=Width-1;y<Height;y++) {
          Data[h]=Data[h+1];
          Data[g]=Data[g-1];
          h+=Width;g+=Width;
        }
      }
      int bitscan(int x) {
       unchecked {
        int r=0;
        ushort u;
        if(x==0) return -1;
        if(0!=(x&0xffff0000)) {r=16;u=(ushort)(x>>16);} else u=(ushort)x;
        if(0!=(u&0xff00)) {r+=8;u>>=8;}
        if(0!=(u&0xf0)) {r+=4;u>>=4;}
        if(0!=(u&0xc)) {r+=2;u>>=2;}
        return r+(u==3?1:u-1);
       }
      }
      
      public static int distance(int mode,int dx,int dy) {
        int d;      
        switch(mode) {
         case 6:d=Math.Abs(dx-dy);break; // \\
         case 5:d=Math.Abs(dx+dy);break; // //
         case 4:d=Math.Abs(dx);break;    // |
         case 3:d=Math.Abs(dy);break;    // -
         case 2:d=Math.Max(Math.Abs(dx),Math.Abs(dy));break; // []
         case 1:d=Math.Abs(dx)+Math.Abs(dy);break; // <>
         default:d=dx*dx+dy*dy;break; // O
        }
        return d;
      }
			public static int distance(int mode,int x,int y,int x0,int y0,int x1,int y1) {
			  x-=x0;y-=y0;
				int dx=x1-x0,dy=y1-y0;
				if(dx==0&&dy==0) return distance(mode,x,y);				
				int a=x*dx+y*dy,d=dx*dx+dy*dy;
				if(a<0) return distance(mode,x,y);
				if(a>d) return distance(mode,x-dx,y-dy);
				int nx=(int)((long)dx*a/d),ny=(int)((long)dy*a/d);
				return distance(mode,x-nx,y-ny);
			}
      public static int distancec(int mode,int x,int y,int x0,int y0,int x1,int y1,bool fill) {        
        int cx=(x0+x1),cy=(y0+y1),rx=2*x0-cx,ry=2*y0-cy,r2=rx*rx+ry*ry,dx=2*x-cx,dy=2*y-cy,d2=dx*dx+dy*dy;
        if(fill&&d2<=r2) return 0;
        if(r2<1||d2<1) return distance(mode,x-x0,y-y0);
        double r=Math.Sqrt(r2*1.0/d2);
        int nx=(int)((cx+r*dx)/2),ny=(int)((cy+r*dy)/2);
        return distance(mode,x-nx,y-ny);
      }
      public static int distanceb(int mode,int x,int y,int x0,int y0,int x1,int y1,bool fill) {
        int d;
        if(x0>x1) {d=x0;x0=y1;x1=d;};
        if(y0>y1) {d=y0;y0=y1;y1=d;};
        int nx=x<x0?x0:x>x1?x1:x,ny=y<y0?y0:y>y1?y1:y,dx,dy;
        if(x==nx&&y==ny) {
          if(fill) return 0;
          nx=2*x<x0+x1?x0:x1;ny=2*y<y0+y1?y0:y1;
          if(Math.Abs(x-nx)<Math.Abs(y-ny)) ny=y;else nx=x;
        }
        dx=x-nx;dy=y-ny;
        switch(mode) {
         case 6:d=x-y<x0-y1?x0-y1-x+y:x-y>x1-y0?x-y-x1+y0:0;break; // \\
         case 5:d=x+y<x0+y0?x0+y0-x-y:x+y>x1+y1?x+y-x1-y1:0;break; // //
         case 4:d=x<x0?x0-x:x>x1?x-x1:0;break;    // |
         case 3:d=y<y0?y0-y:y>y1?y-y1:0;break;    // -
         case 2:d=Math.Max(Math.Abs(dx),Math.Abs(dy));break; // []
         case 1:d=Math.Abs(dx)+Math.Abs(dy);break; // <>
         default:d=dx*dx+dy*dy;break;
        }
        return d;
      }
      public static bool distancex(int mode,int x,int y,int x0,int y0,int x1,int y1,ref int min) {
        int dx=x1-x0,dy=y1-y0,ax=x-x0,ay=y-y0,a=ax*dx+ay*dy,d,n=ax*dy-ay*dx;
        if(a<=0) return n>=0;
        d=dx*dx+dy*dy;
        if(a>d) { ax=x1;ay=y1;}
        else { ax=x0+dx*a/d;ay=y0+dy*a/d;}
        dx=ax-x;dy=ay-y;
        d=distance(mode,x-ax,y-ay);
        if(d<min) min=d;
        return n>=0;
      }
      public static int distancet(int mode,int x,int y,int x0,int y0,int x1,int y1,bool fill) {
        int min=int.MaxValue;
        int dx=x1-x0,dy=y1-y0,x2=x1-dy/2,y2=y1+dx/2,x3=x1+dy/2,y3=y1-dx/2;
        bool inside=distancex(mode,x,y,x0,y0,x2,y2,ref min);
        inside&=distancex(mode,x,y,x2,y2,x3,y3,ref min);
        inside&=distancex(mode,x,y,x3,y3,x0,y0,ref min);
        return inside?fill?0:min:min;
      }  
			public static int distance(int mode,int x,int y,PointPath dxy) {
			  if(dxy.Count<1) return 0;
				PathPoint pp=dxy[dxy.Count-1],pp2;

				int d=distance(mode,x-pp.x,y-pp.y);
			  if(dxy.Count<2) return d;
				pp.stop=!dxy.Closed;
				for(int i=0;i<dxy.Count;i++) { 
				  pp2=pp;pp=dxy[i];
					int d2=pp2.stop?distance(mode,x-pp.x,y-pp.y)
             :pp2.shape==2?distanceb(mode,x,y,pp2.x,pp2.y,pp.x,pp.y,pp2.fill)
             :pp2.shape==1?distancec(mode,x,y,pp2.x,pp2.y,pp.x,pp.y,pp2.fill)
             :pp2.shape==3?distancet(mode,x,y,pp2.x,pp2.y,pp.x,pp.y,pp2.fill)
             :distance(mode,x,y,pp2.x,pp2.y,pp.x,pp.y);
				  //int d2=distance(mode,x-dxy[i],y-dxy[i+1]);
					if(d2<d) d=d2;
				}
				return d;  
			}

      public fillres FloodFill(int[] pxy,int color1,int color2,bool x8,bool noblack,int mode,bool fill2black,PointPath gxy,bool zero,FillPattern pattern,bool down,int mix,int gammax) {
        int dir=mode>6?mode-6:0;
        if(pattern!=null&&(pattern.BMap==null||!pattern.Enabled)) pattern=null;
        if(mode<0&&pxy.Length>4) { int e;e=pxy[0];pxy[0]=pxy[pxy.Length-4];pxy[pxy.Length-4]=e;e=pxy[1];pxy[1]=pxy[pxy.Length-3];pxy[pxy.Length-3]=e;}
        int x=pxy[0],y=pxy[1];
        fillres res=new fillres() {x0=x,y0=y,x1=x,y1=y,m=0};
        if(x<1||x>=Width-1||y<1||y>=Height-1) return res;
        int xy=(y<<16)+x;
        int px=y*Width+x,px2;
        int clr=Data[px];
				int gx=gxy[0].x,gy=gxy[0].y;
				bool multg=gxy.Count>1;
        if(noblack&&(clr&0xffffff)==0) return res;        
        int[] copy=null;
        if(mix>0) { fill2black=true;copy=Data.Clone() as int[];}
        Border(fill2black?0:0x7fffffff);
        int[] fifo=new int[Width*Height];
        int n=0,m=0,max=distance(mode,x,y,gxy),min=zero?0:max,d;
       unsafe{ fixed(int* ff=fifo,pd=Data) {
        ff[m++]=xy;
        pd[px]=-1;
        bool grad=color1!=color2&&(pattern==null||pattern.TrColor>=0)&&mode>=0;
        for(int i=2;i<pxy.Length;i+=2) {
          x=pxy[i];y=pxy[i+1];
          if(x<1||x>=Width-1||y<1||y>=Height-1) return res;          
          xy=(y<<16)+x;
          ff[m++]=xy;
        }        
        while(n<m) {
          xy=ff[n++];
          int x2=xy&65535,y2=(xy>>16),rd;
          bool up=!down||y2>pxy[1];
          if(!grad) d=1;          
          else {
					  d=multg?distance(mode,x2,y2,gxy):distance(mode,x2-gx,y2-gy);
            if(d>max) max=d;else if(d<min) min=d;
          }
          px=y2*Width+x2;
          if(up) {rd=pd[(px2=px-Width)];if(fill2black?rd>0:rd==clr) {ff[m++]=xy-65536;pd[px2]=-1;}}
          rd=pd[(px2=px+Width)];if(fill2black?rd>0:rd==clr) {ff[m++]=xy+65536;pd[px2]=-1;}
          rd=pd[(px2=px-1)];if(fill2black?rd>0:rd==clr) {ff[m++]=xy-1;pd[px2]=-1;}
          rd=pd[(px2=px+1)];if(fill2black?rd>0:rd==clr) {ff[m++]=xy+1;pd[px2]=-1;}
          if(x8) {
            if(up) {
              rd=pd[(px2=px-Width-1)];if(fill2black?rd>0:rd==clr) {ff[m++]=xy-65537;pd[px2]=-1;}
              rd=pd[(px2=px-Width+1)];if(fill2black?rd>0:rd==clr) {ff[m++]=xy-65535;pd[px2]=-1;}
            }
            rd=pd[(px2=px+Width-1)];if(fill2black?rd>0:rd==clr) {ff[m++]=xy+65535;pd[px2]=-1;}
            rd=pd[(px2=px+Width+1)];if(fill2black?rd>0:rd==clr) {ff[m++]=xy+65537;pd[px2]=-1;}
          }
        }
        n=0;
        bool sqr=mode<1||mode>6;        
        int[] cm=null;
        int[] radials=null;
        if(grad) {
          if(sqr) {min=isqrt(min);max=isqrt(max);}
          cm=new int[max-min+1];
          for(int i=0;i<cm.Length;i++) cm[i]=Palette.RGBMix(color1,color2,i,max-min,gammax);        
        }
        if(mode==-2) {
          int gc=gxy.Count;
          int sx=gxy.pt[gc-1].x,sy=gxy.pt[gc-1].y;
          radials=new int[2+Math.Max(1,gxy.Count-1)];
          radials[0]=sx;radials[1]=sy;
          if(gc>1) {    
            sx=gxy.pt[gc-1].x;sy=gxy.pt[gc-1].y;
            for(int i=0;i<gc-1;i++)
              radials[2+i]=(int)(Math.Atan2(gxy.pt[i].y-sy,gxy.pt[i].x-sx)*32768/Math.PI);
          } else 
            radials[2]=(int)(Math.Atan2(pxy[1]-sy,pxy[0]-sx)*32768/Math.PI);
        }
        if(dir>0) {          
          int y0,ye,gs,y2;
          if(dir==4) { y0=-Height+3;ye=Width-2;d=0;gs=Width-1;}
          else if(dir==3) { y0=-Width+3;ye=Height-2;d=0;gs=Width+1;}
          else if(dir==2) { y0=0;ye=Width-2;d=Height-2;gs=Width;}
          else { y0=0;ye=Height-2;d=Width-2;gs=1;}
          for(y2=y0;y2<ye;y2++) {
            int g0=Width+1,g,gi=0,gd=0;
            if(dir==4) { 
              g0+=Width-3-(y2>0?y2:0)+(y2<0?-y2:0)*Width;
              d=y2<0?Math.Min(Height-2+y2,Width-2):Math.Min(Width-2-y2,Height-2);
            } else if(dir==3) { 
              g0+=(y2>0?y2:0)*Width+(y2<0?-y2:0);
              d=y2<0?Math.Min(Width-2+y2,Height-2):Math.Min(Height-2-y2,Width-2);
            } else if(dir==2) { g0+=y2;}
            else {g0+=y2*Width;}
            for(g=g0,gx=0;gx++<d;g+=gs) 
              if(pd[g]<0) {if(gi==0) gi=gx;else gd=gx;}
            if(gi!=0) for(g=g0,gx=0,gd-=gi-1;gx++<d;g+=gs)
              if(pd[g]<0)
                pd[g]=Palette.RGBMix(color1,color2,gx-gi,gd,gammax);            
          }
        }
        while(n<m) {
          xy=ff[n++];
          int x2=xy&65535,y2=(xy>>16);
          if(x2<res.x0) res.x0=x2;else if(x2>res.x1) res.x1=x2;
          if(y2<res.y0) res.y0=y2;else if(y2>res.y1) res.y1=y2;
          if(dir>0) continue;
          px=y2*Width+x2;
          int pdpx;
          if(grad) {
            d=multg?distance(mode,x2,y2,gxy):distance(mode,x2-gx,y2-gy);
            if(sqr) d=isqrt(d);
            //pd[px]=Palette.ColorMix(color1,color2,d-min,max-min);
            pdpx=cm[d-min];
          } else if(mode<0) {
            if(mode==-2) {
              int c,mc,a=(int)(Math.Atan2(y2-radials[1],x2-radials[0])*32768/Math.PI);
              if(radials.Length==3) {
                c=a-radials[2];
                if(c<0) c+=65536;
                if(c>32768) c=65536-c;
                mc=32768;
              } else {
                int lo=a-65536,hi=a+65536;
                for(int i=2;i<radials.Length;i++) {
                  int r=radials[i],rlo=r,rhi=r;
                  if(rlo>a) rlo-=65536;if(rhi<a) rhi+=65536;
                  if(rlo>lo) lo=rlo;
                  if(rhi<hi) hi=rhi;
                }
                mc=hi-lo;
                int mi=lo+hi;
                a*=2;
                if(a<=mi) c=a-2*lo;else c=2*hi-a;
              }
              pdpx=Palette.RGBMix(color1,color2,c,mc,gammax);
            } else {
              int ax=pxy[0],ay=pxy[1],bx=gx,by=gy,cx=bx,cy=by;
              if(gxy.Count>1) {
                ax=bx;ay=gy;cx=bx=gxy.pt[1].x;cy=by=gxy.pt[1].y;
                if(gxy.Count>2) {
                  bool b=gxy[1].stop;
                  cx=gxy.pt[2].y-(b?ay:by);cy=-(gxy.pt[2].x-(b?ax:bx));
                  if(cx*(bx-ax)+cy*(by-ay)<0) {cx=-cx;cy=-cy;}
                  cx+=ax;cy+=ay;
                }
              }
              int dx=x2-ax,dy=y2-ay;
              int sx=cx-ax,sy=cy-ay,sxy=sx*(bx-ax)+sy*(by-ay);
              int dxy=dx*sx+dy*sy;
              pdpx=Palette.RGBMix(color2,color1,dxy,sxy,gammax);
            }
          } else
            pdpx=color1;
          if(mix>0) pdpx=Palette.RGBMix(pdpx,copy[px],mix,255,gammax);
          if(pattern!=null) {
            int rc=pattern.Color(x2,y2);
            if(rc>=0) pdpx=rc;
          }
          pd[px]=pdpx;
        }
       }}
        res.m=m;
        return res;
      }      
      public fillres FloodFill1(int x,int y,bool x8,int[] rect,int incolor,int bcolor,int outcolor) {
			  if(!R.Intersect(rect,0,0,Width-1,Height-1)) return new fillres();
        if(incolor==-1&&bcolor==-1&&outcolor==-1) return new fillres();
        int sx0=0,sy0=0,sx1=Width-1,sy1=Height-1;
				if(rect!=null) {sx0=rect[0];sy0=rect[1];sx1=rect[2];sy1=rect[3];}
        int color=XY(x,y)&White;
        if((incolor<0||incolor==color)&&(bcolor<0||bcolor==color)&&outcolor<0) return new fillres();
        int w=sx1-sx0+1,h=sy1-sy0+1;
        fillres res=new fillres() {x0=x,y0=y,x1=x,y1=y,m=0};
        int[] fifo=new int[w*h];
       unsafe{ fixed(int* ff=fifo,pd=Data) {
        int n=0,m=1;
        ff[0]=(y<<16)|x;        
        while(n<m) {
          int xy=ff[n++],ry=xy>>16,rx=xy&65535,r=ry*Width+rx,r2;
          if(pd[r]==-1) continue;
          pd[r]=-1;
          int mi=0,ma=0;
          while(rx+mi>sx0&&pd[r2=r+mi-1]==color) {mi--;ff[m++]=xy+mi;pd[r2]=-1;}
          while(rx+ma<sx1&&pd[r2=r+ma+1]==color) {ma++;ff[m++]=xy+ma;pd[r2]=-1;}
          if(x8&&rx+mi>sx0) mi--;
          if(x8&&rx+ma<sx1) ma++;
          for(int i=mi;i<=ma;i++) {
            if(ry>sy0&&pd[r2=r-Width+i]==color) {ff[m++]=xy-65536+i;pd[r2]=-2;}
            if(ry<sy1&&pd[r2=r+Width+i]==color) {ff[m++]=xy+65536+i;pd[r2]=-2;}
          }
        }
        if(bcolor<0) bcolor=color;
        if(incolor<0) incolor=color;
        bool oc=outcolor>=0,mark=oc||bcolor>=0&&bcolor!=incolor;        
        if(mark) {bcolor|=(1<<24);incolor|=(1<<24);}
        w=Width;
        for(n=0;n<m;n++) {
          bool b=false;        
          int r=ff[n],rx=r&65535,ry=(r>>16),r2;
          r=ry*Width+rx;
          if(mark) {
            if(rx>sx0&&0==(pd[r2=r-1]&(255<<24))) {b=true;if(oc) pd[r2]=outcolor;}
            if(rx<sx1&&0==(pd[r2=r+1]&(255<<24))) {b=true;if(oc) pd[r2]=outcolor;}
            if(ry>sy0&&0==(pd[r2=r-w]&(255<<24))) {b=true;if(oc) pd[r2]=outcolor;}
            if(ry<sy1&&0==(pd[r2=r+w]&(255<<24))) {b=true;if(oc) pd[r2]=outcolor;}
            if(x8) {
              if(ry>sy0) {
                if(rx>sx0&&0==(pd[r2=r-w-1]&(255<<24))) {b=true;if(oc) pd[r2]=outcolor;}
                if(rx<sx1&&0==(pd[r2=r-w+1]&(255<<24))) {b=true;if(oc) pd[r2]=outcolor;}
              }
              if(ry<sy1) {
                if(rx>sx0&&0==(pd[r2=r+w-1]&(255<<24))) {b=true;if(oc) pd[r2]=outcolor;}
                if(rx<sx1&&0==(pd[r2=r+w+1]&(255<<24))) {b=true;if(oc) pd[r2]=outcolor;}
              }
            }
          }
          pd[r]=b?bcolor:incolor;
          if(mark) ff[n]=r;
        }
        if(mark) for(n=0;n<m;n++) {
          int r=ff[n];
          pd[r]&=White;
        }
       }}
        return res;
      }
      public fillres FloodFill0(int x,int y,bool x8,int[] rect) {
			  if(!R.Intersect(rect,0,0,Width-1,Height-1)) return new fillres();
        int sx0=0,sy0=0,sx1=Width-1,sy1=Height-1;
				if(rect!=null) {sx0=rect[0];sy0=rect[1];sx1=rect[2];sy1=rect[3];}
        int w=sx1-sx0+1,h=sy1-sy0+1;
        int[] fifo=new int[w*h];
        int n=0,m=1;
        fillres res=new fillres() {x0=x,y0=y,x1=x,y1=y,m=0};
       unsafe{ fixed(int* ff=fifo,pd=Data) {
        ff[0]=(y<<16)|x;
        int color=XY(x,y)&White;
        pd[y*Width+x]|=c24;
        ClearByte(sx0,sy0,sx1,sy1,0);
        while(n<m) {
          int xy=ff[n++],ry=xy>>16,rx=xy&65535,r=ry*Width+rx,r2;
          if(rx<res.x0) res.x0=rx;else if(rx>res.x1) res.x1=rx;
          if(ry<res.y0) res.y0=ry;else if(ry>res.y1) res.y1=ry;
          int mi=0,ma=0;          
          while(rx+mi>sx0&&pd[r2=r+mi-1]==color) {mi--;ff[m++]=xy+mi;pd[r2]|=c24;}
          while(rx+ma<sx1&&pd[r2=r+ma+1]==color) {ma++;ff[m++]=xy+ma;pd[r2]|=c24;}
          if(x8&&rx+mi>sx0) mi--;
          if(x8&&rx+ma<sx1) ma++;
          for(int i=mi;i<=ma;i++) {
            if(ry>sy0&&pd[r2=r-Width+i]==color) {ff[m++]=xy-65536+i;pd[r2]|=c24;}
            if(ry<sy1&&pd[r2=r+Width+i]==color) {ff[m++]=xy+65536+i;pd[r2]|=c24;}
          }
        }
       }}
        res.m=m;
        return res;
      }
      public fillres ColorExtent(int x,int y,int[] rect) {
			  if(!R.Intersect(rect,0,0,Width-1,Height-1)) return new fillres();
        int sx0=0,sy0=0,sx1=Width-1,sy1=Height-1;
				if(rect!=null) {sx0=rect[0];sy0=rect[1];sx1=rect[2];sy1=rect[3];}        
        int color=XY(x,y)&White;
        fillres res=new fillres() {y0=x,y1=y,m=0};        
        for(;x>sx0&&(XY(x-1,y)&White)==color;x--);
        res.x0=x;
        for(x=res.y0;x<sx1&&(XY(x+1,y)&White)==color;x++);
        res.x1=x;
        for(x=res.y0;y>sy0&&(XY(x,y-1)&White)==color;y--);
        res.y0=y;
        for(y=res.y1;y<sy1&&(XY(x,y+1)&White)==color;y++);
        res.y1=y;
        return res;
      }
      public byte[] BorderMask(int x0,int y0,int x1,int y1) {
        if(!IntersectRect(ref x0,ref y0,ref x1,ref y1,1,1,Width-2,Height-2)) return null;
        int w=x1-x0+1,h=y1-y0+1;
        if(w<3||h<3) return null;
        byte[] mask=new byte[w*h];
        short[] fifo=new short[2*w*h];
        int n=0,m=0,offset=y0*Width+x0;
        for(int i=0;i<w;i++) mask[i]=mask[w*(h-1)+i]=1;
        for(int i=1;i+1<h;i++) mask[i*w]=mask[i*w+w-1]=1;
        for(int i=1;i+1<w;i++) {
          int p;
          p=offset+i;if((Data[p]&White)==(Data[p+Width]&White)) {mask[i+w]=1;fifo[m++]=(short)i;fifo[m++]=1;}
          p+=(h-1)*Width;if((Data[p]&White)==(Data[p-Width]&White)) {mask[w*(h-2)+i]=1;fifo[m++]=(short)i;fifo[m++]=(short)(h-2);}
        }
        for(int i=2;i+2<h;i++) {
          int p;
          p=offset+i*Width;if((Data[p]&White)==(Data[p+1]&White)) {mask[i*w+1]=1;fifo[m++]=1;fifo[m++]=(short)i;}
          p+=w-1;if((Data[p]&White)==(Data[p-1]&White)) {mask[i*w+w-2]=1;fifo[m++]=(short)(w-2);fifo[m++]=(short)i;}
        }
        while(n<m) {
          int x=fifo[n++],y=fifo[n++],p=offset+y*Width+x,b=y*w+x,b2,c=Data[p]&White;
          if(mask[b2=b-1]==0&&(Data[p-1]&White)==c) { fifo[m++]=(short)(x-1);fifo[m++]=(short)y;mask[b2]=1;}
          if(mask[b2=b+1]==0&&(Data[p+1]&White)==c) { fifo[m++]=(short)(x+1);fifo[m++]=(short)y;mask[b2]=1;}
          if(mask[b2=b-w]==0&&(Data[p-Width]&White)==c) { fifo[m++]=(short)x;fifo[m++]=(short)(y-1);mask[b2]=1;}
          if(mask[b2=b+w]==0&&(Data[p+Width]&White)==c) { fifo[m++]=(short)x;fifo[m++]=(short)(y+1);mask[b2]=1;}
        }
        if(m==2*(w-1)*(h-1)) return null;
        return mask;
      }
      const int c24=0x1000000;
      int FillDiffStep(int[] fifo,int p,int mode,int diff,bool center,int color) {
        int n=0,m=0,p2,c,c2,c0;
        fifo[m++]=p;
        bool rgbavg=color<-1;
        c0=(color<0?Data[p]:color)|c24;
        color=Data[p]&White;
        Data[p]=c0;
        int s0=0,s1=0,s2=0;
        while(n<m) {
          p=fifo[n++];
          c=center?color:Data[p];
          if(rgbavg) Palette.RGBAdd(Data[p],ref s0,ref s1,ref s2);
          else Data[p]=c0;
          if(0==(c24&(c2=Data[p2=p-1]))&&!Palette.RGBDiff(mode,diff,c,c2)) { fifo[m++]=p2;Data[p2]|=c24;}
          if(0==(c24&(c2=Data[p2=p+1]))&&!Palette.RGBDiff(mode,diff,c,c2)) { fifo[m++]=p2;Data[p2]|=c24;}
          if(0==(c24&(c2=Data[p2=p-Width]))&&!Palette.RGBDiff(mode,diff,c,c2)) { fifo[m++]=p2;Data[p2]|=c24;}
          if(0==(c24&(c2=Data[p2=p+Width]))&&!Palette.RGBDiff(mode,diff,c,c2)) { fifo[m++]=p2;Data[p2]|=c24;}
        }
        if(rgbavg) {
          n=0;
          c0=(Palette.RGBDiff(mode,diff,c0,0)?Palette.RGBAvg(m,s0,s1,s2):0)|c24;
          while(n<m)
            Data[fifo[n++]]=c0;
        }
        return m;
      }

      public int FillDiff(int x,int y,int mode,int diff,bool center,int color) {
        if(x<1||x+1>=Width||y<1||y+1>=Height||diff<1) return 0;
        ClearByte();
        Border(c24);
        int[] fifo=new int[Width*Height];
        int s=FillDiffStep(fifo,y*Width+x,mode,diff,center,color);
        ClearByte();
        return s;
      }
      public void FillDiff(int x0,int y0,int x1,int y1,int mode,int diff,bool center,bool rgbavg) {
        if(!IntersectRect(ref x0,ref y0,ref x1,ref y1,1,1,Width-2,Height-2)) return;
        ClearByte(x0,y0,x1,y1,0);
        RectByte(x0-1,y0-1,x1+1,y1+1,1);
        int[] fifo=new int[Width*Height];
        for(int y=y0;y<=y1;y++)
          for(int x=x0,p;x<=x1;x++)
            if(0==(Data[p=y*Width+x]&c24))              
              FillDiffStep(fifo,p,mode,diff,center,rgbavg?-2:-1);
      }
      public void FillDiff(int x0,int y0,int x1,int y1,int dx,int dy) {
        if(!IntersectRect(ref x0,ref y0,ref x1,ref y1,1,1,Width-2,Height-2)) return;
        ClearByte(x0,y0,x1,y1,0);
        RectByte(x0-1,y0-1,x1+1,y1+1,1);
        int[] fifo=new int[(x1-x0+1)*(y1-y0+1)];
        x1-=dx-1;y1-=dy-1;
        for(int y=y0;y<=y1;y++)
          for(int x=x0;x<=x1;x++) {
            int c=XY(x,y);
            if(0==(c&c24)&&c!=0&&c!=White&&IsSolid(x,y,dx,dy))              
              FillDiffStep(fifo,y*Width+x,3,0,true,White);
          }
      }
      public void ReplDiff(int x0,int y0,int x1,int y1,int mode,int diff) {
        if(!IntersectRect(ref x0,ref y0,ref x1,ref y1,1,1,Width-2,Height-2)) return;
        int w=x1-x0+1,h=y1-y0+1,wh=w*h,pe,y,p,i;
        int[] map=new int[wh];
        for(y=y0,i=0;y<=y1;y++)
          for(p=Width*y+x0,pe=p+w;p<pe;p++)
            map[i++]=p;
        for(int n=0,m=map.Length;n<m;) {
          int m2=m,c,s0=0,s1=0,s2=0,c2;
          p=map[n];c=Data[p];
          map[n]=map[--m];map[m]=p;
          Palette.RGBAdd(c,ref s0,ref s1,ref s2);
          for(i=n;i<m;) { 
            p=map[i];
            c2=Data[p];
            if(Palette.RGBDiff(mode,diff,c,c2)) i++;
            else {
              map[i]=map[--m];map[m]=p; 
              Palette.RGBAdd(c2,ref s0,ref s1,ref s2);
            }
          }
          c=Palette.RGBAvg(m2-m,s0,s1,s2);
          while(m2>m) {
            p=map[--m2];
            Data[p]=c;
          }
        }        
      }

      static void Expand1(byte[] data,int width,int height,bool x8,int d8,int count,int count2,int e2) {
        int y,g,ge,cx;
        int d1=d8==3?2:1,d2=d8==1?1:d8==3?3:2,s2=1;
        if(e2>0) { s2+=e2;count+=e2;count2+=e2;}
        if(count>254) count=254;
        if(count>0&&count2>count) {cx=count;count=count2;count2-=cx;} else count2=0;
        for(byte c=(byte)s2;c<=count;c++) {
          for(y=1;y<height-1;y++) {
            byte x,cd1=(byte)(c+d1),cd2=(byte)(c+d2);
            g=y*width+1;
            for(ge=g+width-2;g<ge;g++)  if(data[g]==c) {              
              if(0==(x=data[g+1])||x>cd1) data[g+1]=cd1;
              if(0==(x=data[g-1])||x>cd1) data[g-1]=cd1;
              if(0==(x=data[g-width])||x>cd1) data[g-width]=cd1;
              if(0==(x=data[g+width])||x>cd1) data[g+width]=cd1;
              if(x8||(!x8&&d8!=2)) {
                if(0==data[g-width-1]) data[g-width-1]=cd2;
                if(0==data[g-width+1]) data[g-width+1]=cd2;
                if(0==data[g+width-1]) data[g+width-1]=cd2;
                if(0==data[g+width+1]) data[g+width+1]=cd2;
              }
           }
          }
        }
        if(count2<1) return;
        for(g=0,ge=width*height;g<ge;g++)
          if(e2==0||data[g]==0||data[g]>e2)
            data[g]=(byte)(data[g]==0||data[g]>s2+d1*count?s2:0);
        Expand1(data,width,height,x8,d8,count2,0,e2);
        for(g=0,ge=width*height;g<ge;g++)
          if(e2==0||data[g]==0||data[g]>e2)
            data[g]=(byte)(data[g]==0||data[g]>s2+d1*count?s2:0);
      }

      bool Neq(int c,int p,int lvl,bool lt,bool gt,bool x8) {
        int s=Palette.RGBCmp2(c);
        lvl<<=17;
        int l=s>lvl?s-lvl:0,h=s+lvl;
        int p1=Data[p-1],p2=Data[p+1],p3=Data[p-Width],p4=Data[p+Width];
       x8:
        int s1=Palette.RGBCmp2(p1),s2=Palette.RGBCmp2(p2),s3=Palette.RGBCmp2(p3),s4=Palette.RGBCmp2(p4);
        if(gt&&(s1<l||s2<l||s3<l||s4<l)) return true;
        if(lt&&(s1>h||s2>h||s3>h||s4>h)) return true;
        if(x8) {
          p1=Data[p-Width-1];p2=Data[p-Width+1];p3=Data[p+Width-1];p4=Data[p+Width+1];
          x8=false;
          goto x8;
        }
        return false;
      }

      public void Neq(int lvl,bool lt,bool gt,bool x8,int d8,int expand,int expand2,int color,int x0,int y0,int x1,int y1) {
        if(!IntersectRect(ref x0,ref y0,ref x1,ref y1,1,1,Width-2,Height-2)) return ;
        int w=x1-x0+1,h=y1-y0+1,x,y,p,g;
        byte[] data=new byte[w*h];
        for(y=y0,g=0;y<=y1;y++)
          for(x=x0,p=y*Width+x0;x<=x1;x++,p++,g++) {
            int c=Data[p]&White;
            if(expand<1&&(c==0||c==White)) continue;
            if(Neq(c,p,lvl,lt,gt,x8))
              data[g]=1;
          }
        Expand1(data,w,h,x8,d8,expand,expand2,0);
        for(y=y0,g=0;y<=y1;y++)
          for(x=x0,p=y*Width+x0;x<=x1;x++,p++,g++) {
            int c;
            if(data[g]==0&&((c=Data[p]&White)!=0&&c!=White)) Data[p]=color;          
          }
      }

      public bool Outline(int x,int y,int count,bool d8, bool x8,int color,int exp,int x0,int y0,int x1,int y1) {
        if(!IntersectRect(ref x0,ref y0,ref x1,ref y1,1,1,Width-2,Height-2)||x<x0||x>x1||y<y0||y>y1) return false;
        int c=XY(x,y)&White;
        if(c==color) return false;
        int i,j,w2=x1-x0+1+2,h2=y1-y0+1+2,p=Width*y0+x0,h,g,h3,d;
        byte[] data=new byte[w2*h2];
        for(j=0;j<h2-2;j++)          
          for(i=0,h=(y0+j)*Width+x0,g=(j+1)*w2+1;i<w2-2;i++,h++,g++)
             data[g]=(byte)((Data[h]&White)!=c?1:0);        
        for(i=0;i<w2;i++) data[i]=data[w2*h2-1-i]=1;
        for(j=0;j<h2;j++) data[j*w2]=data[j*w2+w2-1]=1;

        if(exp>0) {
          Expand1(data,w2,h2,x8,x8?1:2,exp,0,0);
          for(h=0;h<w2*h2;h++) if(data[h]!=0) data[h]=1;
        }

        for(i=1,h=(y-y0+1)*w2+(x-x0+1);;i++) {
          if(data[g=h-i*w2]!=0) {h=g+w2;d=0;break;}
          if(data[g=h+i*w2]!=0) {h=g-w2;d=2;break;}
          if(data[g=h+i]!=0) {h=g-1;d=1;break;}
          if(data[g=h-i]!=0) {h=g+1;d=3;break;}
        }

        g=h;
        do {
          data[h]=2;
          for(i=0;i<4;i++,d=(d+1)&3) {
            if((data[h3=h+(d==0?-w2:d==1?1:d==2?+w2:-1)]!=1)) {
              if(d8) {
                int h4=h3+(d==0?-1:d==1?-w2:d==2?+1:+w2);
                if(data[h4]!=1) {
                  h3=h4;
                  d=(d+3)&3;
                }
              }
              h=h3;
              d=(d+3)&3;
              break;
            }
          }
        } while(h!=g);

        if(count>0) Expand1(data,w2,h2,x8,x8?1:2,count,0,1);
        for(j=0;j<h2-2;j++)
          for(i=0,h=(y0+j)*Width+x0,g=(j+1)*w2+1;i<w2-2;i++,h++,g++)
              if(data[g]>1) Data[h]=color;
        
        return true;        
      }


      public fillres RemoveColor(int color,bool x8,bool repeat,int x0,int y0,int x1,int y1) {
        fillres res=new fillres() {x0=x0,y0=y0,x1=x1,y1=y1,m=0};
        if(!IntersectRect(ref x0,ref y0,ref x1,ref y1,1,1,Width-2,Height-2)) return res;
        Border(color);
        color&=White;
        ClearByte(x0,y0,x1,y1,true,color);
        RectByte(x0-1,y0-1,x1+1,y1+1,1);
        int c24=1<<24,c25=1<<25,h2,h=0,m=0,he;
        int[] fifo=new int[(x1-x0+1)*(y1-y0+1)];
        for(int px,y=y0;y<=y1;y++) {
          px=y*Width+x0;
          for(int x=x0;x<=x1;x++,px++) {
            int c=Data[px];
            if(0!=(c&c24)) continue;
            bool edge=0!=(c24&(Data[px-1]|Data[px+1]|Data[px-Width]|Data[px+Width]));
            if(!edge&&x8) edge=0!=(c24&(Data[px-Width-1]|Data[px-Width+1]|Data[px+Width-1]|Data[px+Width+1]));
            if(edge) {fifo[m++]=px;Data[px]|=c25;}
          }
        }
       loop:
        for(h2=h,he=m;h<he;) {
          int px2,px=fifo[h++];
          int s0=0,s1=0,s2=0,c2,n=0;              
          if(((c2=Data[px2=px-1])&c24)!=0) {n++;Palette.RGBAdd(c2,ref s0,ref s1,ref s2);} else if(0==(c2&c25)) {fifo[m++]=px2;Data[px2]|=c25;}
          if(((c2=Data[px2=px+1])&c24)!=0) {n++;Palette.RGBAdd(c2,ref s0,ref s1,ref s2);} else if(0==(c2&c25)) {fifo[m++]=px2;Data[px2]|=c25;}
          if(((c2=Data[px2=px-Width])&c24)!=0) {n++;Palette.RGBAdd(c2,ref s0,ref s1,ref s2);} else if(0==(c2&c25)) {fifo[m++]=px2;Data[px2]|=c25;}
          if(((c2=Data[px2=px+Width])&c24)!=0) {n++;Palette.RGBAdd(c2,ref s0,ref s1,ref s2);} else if(0==(c2&c25)) {fifo[m++]=px2;Data[px2]|=c25;}
          if(x8) {
            if(((c2=Data[px2=px-Width-1])&c24)!=0) {n++;Palette.RGBAdd(c2,ref s0,ref s1,ref s2);} else if(0==(c2&c25)) {fifo[m++]=px2;Data[px2]|=c25;}
            if(((c2=Data[px2=px-Width+1])&c24)!=0) {n++;Palette.RGBAdd(c2,ref s0,ref s1,ref s2);} else if(0==(c2&c25)) {fifo[m++]=px2;Data[px2]|=c25;}
            if(((c2=Data[px2=px+Width-1])&c24)!=0) {n++;Palette.RGBAdd(c2,ref s0,ref s1,ref s2);} else if(0==(c2&c25)) {fifo[m++]=px2;Data[px2]|=c25;}
            if(((c2=Data[px2=px+Width+1])&c24)!=0) {n++;Palette.RGBAdd(c2,ref s0,ref s1,ref s2);} else if(0==(c2&c25)) {fifo[m++]=px2;Data[px2]|=c25;}
          }
          if(n==0) Data[px]=c25;
          else Data[px]=Palette.RGBAvg(n,s0,s1,s2)|c25;
        }
        for(h=h2;h<he;)
          Data[fifo[h++]]^=c24|c25;
        if(repeat&&m>he) goto loop;
        ClearByte(x0-1,y0-1,x1+1,y1+1,0);
        return res;
      }
      public fillres Replace(int search,int replace,int x0,int y0,int x1,int y1) { return Replace(search,replace,x0,y0,x1,y1,null);}
      public fillres Replace(int search,int replace,int x0,int y0,int x1,int y1,FillPattern pattern) {
        fillres res=new fillres() {x0=x0,y0=y0,x1=x1,y1=y1,m=0};
        if(!IntersectRect(ref x0,ref y0,ref x1,ref y1,1,1,Width-2,Height-2)) return res;
        int px,clr=search&White;
        for(int y=y0;y<=y1;y++) {
          px=y*Width+x0;
          for(int x=x0;x<=x1;x++,px++) {
            int c=Data[px]&White;
            if(c!=clr) continue;
            if(pattern!=null) {
              c=pattern.Color(x,y);
              if(c<0) continue;
              Data[px]=c;
            } else {
              Data[px]=replace;
            }
            res.Add(x,y);
          }
        }
        return res;
      }
      public void ReplaceStrip(bool vert,int xy,int replace,int x0,int y0,int x1,int y1) {
        if(!IntersectRect(ref x0,ref y0,ref x1,ref y1,1,1,Width-2,Height-2)) return;
        int l=vert?Height:Width;
        if(xy<0) xy=0;else if(xy>=l) xy=l-1;
        if(vert)
          for(int x=x0;x<=x1;x++)
            Replace(XY(x,xy),replace,x,y0,x,y1);
        else
          for(int y=y0;y<=y1;y++)
            Replace(XY(xy,y),replace,x0,y,x1,y);
      }
      public fillres ReplaceDiff(int search,int replace,int mode,int level,int x0,int y0,int x1,int y1) {
        fillres res=new fillres() {x0=x0,y0=y0,x1=x1,y1=y1,m=0};
        if(!IntersectRect(ref x0,ref y0,ref x1,ref y1,1,1,Width-2,Height-2)) return res;
        int px,clr=search&White;
        for(int y=y0;y<=y1;y++) {
          px=y*Width+x0;
          for(int x=x0;x<=x1;x++,px++) {
            int c=Data[px]&White;
            if(Palette.RGBDiff(mode,level,c,clr)) continue;
            Data[px]=replace;
            if(res.m==0) { 
              res.x0=res.x1=x;res.y0=res.y1=y;
            } else {
              if(x<res.x0) res.x0=x;else if(x>res.x1) res.x1=x;
              if(y<res.y0) res.y0=y;else if(y>res.y1) res.y1=y;              
            }
            res.m++;
          }
        }
        return res;
      }
      public fillres Replace(int search,bool x8,int incolor,int bcolor,int outcolor,bmap src,int x0,int y0,int x1,int y1) {
        if(outcolor==search) outcolor=-1;if(incolor==search) incolor=-1;if(bcolor==search) bcolor=-1;
        if(outcolor==-1&&incolor==bcolor) return Replace(search,incolor,x0,y0,x1,y1);
        fillres res=new fillres() {x0=x0,y0=y0,x1=x1,y1=y1,m=0};
        if(!IntersectRect(ref x0,ref y0,ref x1,ref y1,1,1,Width-2,Height-2)) return res;
        int px;
        search&=White;
        src.ClearByte();
        for(int y=y0;y<=y1;y++) {
          px=y*Width+x0;
          for(int x=x0;x<=x1;x++,px++) {
            int c=Data[px]&White;
            if(c!=search) {
              if(outcolor<0) continue;
              bool o=src.Data[px-1]==search||src.Data[px+1]==search||src.Data[px-Width]==search||src.Data[px+Width]==search;
              if(!o&&x8) o=src.Data[px-Width-1]==search||src.Data[px-Width+1]==search||src.Data[px+Width-1]==search||src.Data[px+Width+1]==search;
              if(o) Data[px]=outcolor;
            } else {
              bool b=src.Data[px-1]!=search||src.Data[px+1]!=search||src.Data[px-Width]!=search||src.Data[px+Width]!=search;
              if(!b&&x8) b=src.Data[px-Width-1]!=search||src.Data[px-Width+1]!=search||src.Data[px+Width-1]!=search||src.Data[px+Width+1]!=search;
              if(b&&bcolor>=0) Data[px]=bcolor;
              else if(!b&&incolor>=0) Data[px]=incolor;
              else continue;
            }
            if(res.m==0) { 
              res.x0=res.x1=x;res.y0=res.y1=y;
            } else {
              if(x<res.x0) res.x0=x;else if(x>res.x1) res.x1=x;
              if(y<res.y0) res.y0=y;else if(y>res.y1) res.y1=y;              
            }
            res.m++;
          }
        }
        return res;
      }
      public fillres Replace(int x,int y,int color1,int color2,bool noblack,int mode,int gx,int gy,bool zero,int x0,int y0,int x1,int y1,bool bmask) {
        fillres res=new fillres() {x0=x0,y0=y0,x1=x1,y1=y1,m=0};
        if(x<1||x>=Width-1||y<1||y>=Height-1) return res;
        if(!IntersectRect(ref x0,ref y0,ref x1,ref y1,1,1,Width-2,Height-2)) return res;
        int px=y*Width+x,d,min=0,max=0,w=x1-x0+1;
        int clr=Data[px]&White;
        if(noblack&&clr==0) return res;
        byte[] mask=null;
        if(bmask) {
          mask=BorderMask(x0,y0,x1,y1);
          if(mask==null) return res;
        }
        for(y=y0;y<=y1;y++) {
          px=y*Width+x0;
          for(x=x0;x<=x1;x++,px++) {
            if(mask!=null&&mask[x-x0+(y-y0)*w]==1) continue;
            int c=Data[px]&White;
            if(c!=clr) continue;
            d=distance(mode,x-gx,y-gy);
            if(res.m==0) {
              min=zero?0:d;              
              max=d;
              res.x0=res.x1=x;res.y0=res.y1=y;
            } else {
              if(d>max) max=d;
              else if(d<min) min=d;
              if(x<res.x0) res.x0=x;else if(x>res.x1) res.x1=x;
              if(y<res.y0) res.y0=y;else if(y>res.y1) res.y1=y;
            }
            res.m++;
          }
        }
        bool sqr=mode<1||mode>6;        
        if(sqr) {min=isqrt(min);max=isqrt(max);}
        int[] cm=new int[max-min+1];
        for(int i=0;i<cm.Length;i++) cm[i]=Palette.RGBMix(color1,color2,i,max-min);                
        for(y=y0;y<=y1;y++) {
          px=y*Width+x0;
          for(x=x0;x<=x1;x++,px++) {
            if(mask!=null&&mask[x-x0+(y-y0)*w]==1) continue;
            int c=Data[px]&White;
            if(c!=clr) continue;
            d=distance(mode,x-gx,y-gy);
            if(sqr) d=isqrt(d);
            Data[px]=cm[d-min];
          }
        }        
        return res;  
      }

      public void ReplaceCirc(short[] xy,short[] ma,int g0,int h,int w,int dh,int dw) {
        int x,xe,y,ye,g,j1,sh=Width,sw=1;
        short f,fe,fp;
        for(x=0,y=dh<0?h:-1;x<w;x++) ma[x]=(short)y;
        for(y+=dh,ye=y+h*dh;y!=ye;y+=dh) {
          f=fe=0;xy[fe++]=(short)(x=dw<0?w:-1);xy[fe++]=(short)y;
          for(x+=dw,xe=x+w*dw,g=g0+y*sh+x*sw;x!=xe;x+=dw,g+=dw*sw) {
            if((j1=Data[g])<0) {
              int dx,dy,dd=0,mm=1<<30,m=ma[x];
              if(dh>0) while(f<fe&&xy[fe-1]<=m) fe-=2;
              else while(f<fe&&xy[fe-1]>=m) fe-=2;
              xy[fe++]=(short)x;xy[fe++]=(short)m;
              for(fp=f;fp<fe;fp+=2) {
                dx=xy[fp]-x;dy=xy[fp+1]-y;dd=dx*dx+dy*dy;
                if(dd>mm) continue;
                mm=dd;f=fp;
              }
              if(-mm>j1) Data[g]=-mm;
            } else {
              ma[x]=(short)y;
              f=fe=0;xy[fe++]=(short)x;xy[fe++]=(short)y;
            }
          }
        }

      }

      public void NegMinMax(int x0,int y0,int x1,int y1,out int ni,out int na) {
        ni=0;na=int.MinValue;
        for(int y=y0;y<=y1;y++) 
          for(int x=x0,g=y*Width+x,c;x<=x1;x++,g++)
            if((c=Data[g])<0) {
              if(c<ni) ni=c;
              if(c>na) na=c;
            }
      }
      public void NegColor(int color1,int color2,int cmode,int shift,bool sqrt,int x0,int y0,int x1,int y1) {
        int ni,na,max;
        bool b64=cmode==1;
        if(b64) max=255;
        else {
          NegMinMax(x0,y0,x1,y1,out ni,out na);
          max=-ni;
          if(shift>0) max>>=shift;
          if(sqrt) max=isqrt(max);
        }
        for(int y=y0;y<=y1;y++) 
          for(int x=x0,g=y*Width+x,c;x<=x1;x++,g++)
            if((c=Data[g])<0) {
              c=-c; 
              if(shift>0) c>>=shift;
              if(sqrt) c=isqrt(c);
              if(b64) c=(c&63)<<2|((c>>4)&3);
              Data[g]=Palette.RGBMix(color1,color2,c,max);
            }
      }
      public void ReplaceCirc(int search,int color1,int color2,int cmode,int x0,int y0,int x1,int y1) {
        if(!IntersectRect(ref x0,ref y0,ref x1,ref y1,1,1,Width-2,Height-2)) return ;
        ClearByte(x0,y0,x1,y1,0);
        Replace(search,int.MinValue,x0,y0,x1,y1);
        int h=y1-y0+1,w=x1-x0+1,x2=Math.Max(w,h)+1,g0=y0*Width+x0;
        short[] ma=new short[x2],xy=new short[2*x2];
        ReplaceCirc(xy,ma,g0,h,w,+1,1);
        ReplaceCirc(xy,ma,g0,h,w,-1,1);
        ReplaceCirc(xy,ma,g0,h,w,+1,-1);
        ReplaceCirc(xy,ma,g0,h,w,-1,-1);
        NegColor(color1,color2,cmode,0,true,x0,y0,x1,y1);
      }

      int ceq4n(int p,int step) {
        int count=1;
        for(;Data[p+=step]<0;count++);
        return count;
      }

      void fill9x(int p,int j,int step,char dd) {
        int r,d=dd==2?8:dd==3?12:4;
        for(;(r=Data[p])<0;j-=d,p+=step) if(r<j) Data[p]=j;
      }

      int fill9a(int k,int dx,int dy,char dd) {
       int n=ceq4n(k,dx),k1=k,k2=k+dx*(n-1),j1=dx==1?-4:-5,j2=j1-2,r;
       byte d1=(byte)(j1&3),d2=(byte)(j2&3),dz=(byte)(dd==3?8:4);
       for(;k2>=k;j1-=dz,j2-=dz,k1+=dx,k2-=dx) {
         if(dd==2) {
           if((r=Data[k1+dy])<0&&r-4>j1) j1=r-4;
           if((r=Data[k2+dy])<0&&r-4>j2) j2=r-4;
           if((r=Data[k1+dy-dx])<0&&r-8>j1) j1=r-8;
           if((r=Data[k2+dy+dx])<0&&r-8>j2) j2=r-8;
         } else if(dd==3) {
           if((r=Data[k1+dy])<0&&r-8>j1) j1=r-8;
           if((r=Data[k2+dy])<0&&r-8>j2) j2=r-8;
           if((r=Data[k1+dy-dx])<0&&r-12>j1) j1=r-12;
           if((r=Data[k2+dy+dx])<0&&r-12>j2) j2=r-12;
         }
         if(Data[k1]<j1) Data[k1]=j1;if(Data[k2]<j2) Data[k2]=j2;
         if(dd==1) {
           if((r=Data[k1+dy])<0&&r>j1&&((r&3)==d1)) j1=r;
           if((r=Data[k2+dy])<0&&r>j2&&((r&3)==d2)) j2=r;
         }
       }
       return n;
      }

      public void ReplaceFill(int search,int color1,int color2,int cmode,int x3,int x0,int y0,int x1,int y1) {
        if(!IntersectRect(ref x0,ref y0,ref x1,ref y1,1,1,Width-2,Height-2)) return ;
        ClearByte(x0-1,y0-1,x1+1,y1+1,0);
        Replace(search,int.MinValue,x0,y0,x1,y1);
        int w2=Width,k,x,y,h=y1-y0+1,w=x1-x0+1,x2=Math.Max(w,h)+1,g0=y0*w2+x0,ge=g0+h*w2;
        char dd=(char)x3;
        for(y=h;y--!=0;) 
         for(k=g0+y*w2,ge=k+w;k<ge;k++) 
           if(Data[k]<0) k+=fill9a(k,1,w2,dd);           
        for(y=0;y<h;y++) 
         for(k=g0+y*w2,ge=k+w;k<ge;k++) 
           if(Data[k]<0) k+=fill9a(k,1,-w2,dd);
        for(x=w;x--!=0;) 
         for(k=g0+x,ge=k+h*w2;k<ge;k+=w2) 
           if(Data[k]<0) k+=w2*fill9a(k,w2,1,dd);
        for(x=0;x<w;x++) 
         for(k=g0+x,ge=k+h*w2;k<ge;k+=w2) 
           if(Data[k]<0) k+=w2*fill9a(k,w2,-1,dd);
        for(y=h;y--!=0;) 
         for(k=g0+y*w2,ge=k+w;k<ge;k++) 
           if(Data[k]<0) {
             if(Data[k+w2]<0&&Data[k+w2-1]<0) fill9x(k+w2,-8,w2+1,dd);
             if(Data[k+-w2]<0&&Data[k-w2-1]<0) fill9x(k-w2,-8,-w2+1,dd);
             k+=ceq4n(k,1);
             if(Data[k+w2-1]<0&&Data[k+w2]<0) fill9x(k+w2-1,-10,+w2-1,dd);
             if(Data[k-w2-1]<0&&Data[k-w2]<0) fill9x(k-w2-1,-10,-w2-1,dd);
           }
        if(cmode==100) {
          int rgb=(color1>>7)&0x10101,c,g;
          for(y=y0;y<=y1;y++) 
            for(x=x0,g=y*Width+x;x<=x1;x++,g++)
            if((c=Data[g])<0) {
              c&=3;
              if(c>1) c^=1;
              Data[g]=((c<<6)+32)*rgb;
            }
        } else
          NegColor(color1,color2,cmode,2,false,x0,y0,x1,y1);
      }

      public int beq(byte[] data,int step,int i) {
        byte x=data[i];
        int n=1;
        while(data[i+=step]==x) n++;
        return n;
      }
      public void Fall(int color,int lim,bool d8,bool exp,int x0,int y0,int x1,int y1) {
        if(!IntersectRect(ref x0,ref y0,ref x1,ref y1,1,1,Width-2,Height-2)) return ;
        ClearByte(x0,y0,x1,y1,0);
        RectByte(x0-1,y0-1,x1+1,y1+1,1);
        int w=x1-x0+1,w2=w+2,h=y1-y0+1,xx,yy,y,g0=y0*Width+x0,g,ge,p,pe,p0=w2+1,p1,m,n,s,r,i,j,m2,m3,j2,k,jm;
        int[] fifo=new int[w*h];
        byte[] mem=new byte[w2*(h+2)];
        for(y=0;y<h;y++)
          for(g=g0+y*Width,ge=g+w,p=p0+y*w2;g<ge;g++,p++)
            mem[p]=(byte)(Data[g]==color?1:2);
        if(exp)
          for(y=0;y<h;y++)
            for(g=g0+y*Width,ge=g+w,p=p0+y*w2;g<ge;g++,p++)
              if(mem[p]==1&&(Data[g-1]==color||Data[g+1]==color||Data[g+Width]==color||Data[g-Width]==color))
                mem[p]=2;
        for(y=h;y-->0;)
          for(p1=p=p0+y*w2,pe=p+w;p<pe;p++)
            if(mem[p]==2) {              
              m=1;fifo[m2=n=0]=(y<<16)+(xx=p-p1);
              mem[p0+y*w2+xx]=3;
             fill:
              while(n<m) {
                j=fifo[n++];
                r=p0+(yy=j>>16)*w2+(xx=j&65535);
                if(mem[s=r+1]==2) {fifo[m++]=j+1;mem[s]=3;}
                if(mem[s=r-1]==2) {fifo[m++]=j-1;mem[s]=3;}
                if(mem[s=r+w2]==2) {fifo[m++]=j+0x10000;mem[s]=3;}
                if(mem[s=r-w2]==2) {fifo[m++]=j-0x10000;mem[s]=3;}
                if(d8) {
                  if(mem[s=r+w2-1]==2) {fifo[m++]=j+0xffff;mem[s]=3;}
                  if(mem[s=r+w2+1]==2) {fifo[m++]=j+0x10001;mem[s]=3;}
                  if(mem[s=r-w2-1]==2) {fifo[m++]=j-0x10001;mem[s]=3;}
                  if(mem[s=r-w2+1]==2) {fifo[m++]=j-0xffff;mem[s]=3;}
                }
              }
              m3=m;if(m2==0) m2=m;
              j2=j=h-1-y;
              if(j>0) for(n=0;n<m;) {
                s=fifo[n++];
                r=p0+(yy=s>>16)*w2+(xx=s&65535);
                if((k=mem[r+w2])!=3) {
                  if(k!=1&&k!=2) {j=0;break;}
                  if(k==1) {
                    i=beq(mem,w2,r+w2);
                    k=mem[r+w2*(i+1)];
                    if(k==3) continue;
                  } else i=0;
                  if(k==2) {
                    fifo[m3++]=((yy+i+1)<<16)+xx;
                    if(i<j2) j2=i;
                  } else if(i<j) if((j=i)<=lim) break;
                }
              }
              j2-=lim;j-=lim;
              if(j<=0) {
                for(n=0;n<m;) {
                  s=fifo[n++];
                  r=p0+(yy=s>>16)*w2+(xx=s&65535);
                  if(n<=m2) jm=4;
                  else {jm=2;
                    if(yy>y) {y=yy;p1=p0+y*w2;p=p1+xx-1;pe=p+w;}
                  }
                  mem[r]=(byte)jm;
                }
                continue;
              }
              Array.Sort(fifo,0,m);
              if(j<j2) j2=j;
              jm=j2<j&&m<m3?3:4;
              if(j2>0) for(n=m;n>0;) {
                s=fifo[--n];
                if(jm==3||m2<=n) fifo[n]=s+j2*0x10000;
                r=p0+(yy=s>>16)*w2+(xx=s&65535);
                mem[r]=1;
                mem[r+j2*w2]=(byte)jm;
                r=g0+yy*Width+xx;
                Data[r+j2*Width]=Data[r];
                Data[r]=color;
              }
              if(jm==3) {
                n=m;
                while(m<m3) {
                  s=fifo[m++];
                  r=p0+(yy=s>>16)*w2+(xx=s&65535);
                  mem[r]=3;
                }
                goto fill;
              }
              while(m2<m) {
                s=fifo[m2++];
                r=p0+(yy=s>>16)*w2+(xx=s&65535);
                mem[r]=3;
                if(yy>y) {y=yy;p1=p0+y*w2;p=p1+xx-1;pe=p+w;}
              }
            }        
      }

      public void Colorize(int cmode,int color1,int color2,int gmode,int gx,int gy,bool zero,int x0,int y0,int x1,int y1,bool bmask) {
        if(!IntersectRect(ref x0,ref y0,ref x1,ref y1,1,1,Width-2,Height-2)) return;
        int x,y,px,d,min=0,max=0,w=x1-x0+1;
        if(x0<=gx&&gx<=x1&&y0<=gy&&gy<=y1) zero=true;
        byte[] mask=null;
        if(bmask) {
          mask=BorderMask(x0,y0,x1,y1);
          if(mask==null) return;
        }
        bool first=true;
        //d=distance(gmode,x0-gx,y0-gy);
        //max=d;min=zero?0:d;
        for(y=y0;y<=y1;y++) {
          px=y*Width+x0;
          for(x=x0;x<=x1;x++,px++) {
            if(mask!=null&&mask[x-x0+(y-y0)*w]==1) continue;
            d=distance(gmode,x-gx,y-gy);
            if(first) { first=false;max=d;min=zero?0:d;}
            else {
              if(d>max) max=d;else if(!zero&&d<min) min=d;
            }
          }
        }
        bool sqr=gmode<1||gmode>6;        
        if(sqr) {min=isqrt(min);max=isqrt(max);}
        int[] cm=new int[max-min+1];
        for(int i=0;i<cm.Length;i++) cm[i]=Palette.RGBMix(color1,color2,i,max-min);                
        for(y=y0;y<=y1;y++) {
          px=y*Width+x0;
          for(x=x0;x<=x1;x++,px++) {
            if(mask!=null&&mask[x-x0+(y-y0)*w]==1) continue;
            d=distance(gmode,x-gx,y-gy);
            if(sqr) d=isqrt(d);
            int c=cm[d-min],p=Data[px]&White;
            Data[px]=Palette.Colorize(cmode,c,p);
          }
        }        
      }      
      

      public int[] CreateFifo(int[] xy,int d,bool noblack,out int m) {
        int[] fifo=new int[Width*Height];
        m=0;
        if(xy!=null) for(int i=0,x,y,px,c;i<xy.Length-1;i+=2) {
          x=xy[i];y=xy[i+1];
          if(x>0&&x<Width-1&&y>0&&y<Height-1&&(c=Data[px=y*Width+x])!=d&&(!noblack||c!=0))
            Data[fifo[m++]=px]=d;  
        }
        return fifo;
      }

      // path gradient dx=1 box,dx=2 diam,dx=3 octa
      public int FloodFillGrad(int[] xy,int color1,int color2,int border,int dx,bool x8,bool noblack,bool fill2black,bool down,int limit,bmap undo,int gammax) {
        int x=xy[0],y=xy[1];
        if(x<1||x>=Width-1||y<1||y>=Height-1) return 0;
        int px=y*Width+x,px2;
        int clr=Data[px];
        if(noblack&&(clr&0xffffff)==0) return 0;
        Border(fill2black?0:0x7fffffff);
        int n=0,m=0,k=0;
        int d=-1,md=d;
        int[] fifo=CreateFifo(xy,d,noblack,out m);
        bool dual=!(dx==1&&x8||dx==2&&!x8)&&border<1;
        k=m;
        d--;
        while(n<m) {
          px=fifo[n++];
          int d2=Data[px]-(dx==3?2:1);
          if(dual&&d2!=d) k=m;
          d=d2;
          bool fl=false,fr=false,ft=false,fb=false;
          int rd;
          bool up=!down||px/Width>xy[1];
          if(up) {rd=Data[(px2=px-Width)];if(fill2black?rd>0:rd==clr) {if(dual) { if(k==m) m++;else fifo[m++]=fifo[k];fifo[k++]=px2;} else fifo[m++]=px2;Data[px2]=d;ft=true;}}
          rd=Data[(px2=px+Width)];if(fill2black?rd>0:rd==clr) {if(dual) { if(k==m) m++;else fifo[m++]=fifo[k];fifo[k++]=px2;} else fifo[m++]=px2;Data[px2]=d;fb=true;}
          rd=Data[(px2=px-1)];if(fill2black?rd>0:rd==clr) {if(dual) { if(k==m) m++;else fifo[m++]=fifo[k];fifo[k++]=px2;} else fifo[m++]=px2;Data[px2]=d;fl=true;}
          rd=Data[(px2=px+1)];if(fill2black?rd>0:rd==clr) {if(dual) { if(k==m) m++;else fifo[m++]=fifo[k];fifo[k++]=px2;} else fifo[m++]=px2;Data[px2]=d;fr=true;}
          if(x8||dx!=2) {
            if(dx!=1) d--;
            if(x8&&dx==2) {
              if(!fl&&Data[px-1]<0) fl=true;
              if(!fr&&Data[px+1]<0) fr=true;
              if(!ft&&Data[px-Width]<0) ft=true;
              if(!fb&&Data[px+Width]<0) fb=true;
            }            
            if(up) {
              rd=Data[(px2=px-Width-1)];if((fill2black?rd>0:rd==clr)&&(x8?dx==1||!ft&&!fl:ft||fl)) {fifo[m++]=px2;Data[px2]=d;}
              rd=Data[(px2=px-Width+1)];if((fill2black?rd>0:rd==clr)&&(x8?dx==1||!ft&&!fr:ft||fr)) {fifo[m++]=px2;Data[px2]=d;}
            }
            rd=Data[(px2=px+Width-1)];if((fill2black?rd>0:rd==clr)&&(x8?dx==1||!fb&&!fl:fb||fl)) {fifo[m++]=px2;Data[px2]=d;}
            rd=Data[(px2=px+Width+1)];if((fill2black?rd>0:rd==clr)&&(x8?dx==1||!fb&&!fr:fb||fr)) {fifo[m++]=px2;Data[px2]=d;}
            if(dx!=1) d++;
          }
        }
        if(border>0) {
          n=0;
          d=-1;
          int clb;
          unchecked { clb=(int)0x80000001;}         
          for(int i=0;i<m;i++) {
            px=fifo[i];
            bool go=Data[px-1]>=0||Data[px+1]>=0||Data[px+Width]>=0||Data[px-Width]>=0;
            if(!go&&x8&&(Data[px-Width-1]>=0||Data[px-Width+1]>=0||Data[px+Width-1]>=0||Data[px+Width+1]>=0)) go=true;
            if(go) {
              fifo[n++]=px;
              Data[px]=d;
            } else
              Data[px]=clb;
          }
          k=m=n;
          n=0;
          dual=!(dx==1&&x8||dx==2&&!x8);
          d--;
          while(n<m) {
            px=fifo[n++];
            int d2=Data[px]-(dx==3?2:1);
            if(dual&&d2!=d) k=m;
            d=d2;
            bool fl=false,fr=false,ft=false,fb=false;
            if(Data[(px2=px-Width)]==clb) {if(dual) { if(k==m) m++;else fifo[m++]=fifo[k];fifo[k++]=px2;} else fifo[m++]=px2;Data[px2]=d;ft=true;};
            if(Data[(px2=px+Width)]==clb) {if(dual) { if(k==m) m++;else fifo[m++]=fifo[k];fifo[k++]=px2;} else fifo[m++]=px2;Data[px2]=d;fb=true;};
            if(Data[(px2=px-1)]==clb) {if(dual) { if(k==m) m++;else fifo[m++]=fifo[k];fifo[k++]=px2;} else fifo[m++]=px2;Data[px2]=d;fl=true;};
            if(Data[(px2=px+1)]==clb) {if(dual) { if(k==m) m++;else fifo[m++]=fifo[k];fifo[k++]=px2;} else fifo[m++]=px2;Data[px2]=d;fr=true;};
            if(x8||dx!=2) {
              if(dx!=1) d--;
              if(x8&&dx==2) {
                if(!fl&&Data[px-1]<0) fl=true;
                if(!fr&&Data[px+1]<0) fr=true;
                if(!ft&&Data[px-Width]<0) ft=true;
                if(!fb&&Data[px+Width]<0) fb=true;
              }
              if(Data[(px2=px-Width-1)]==clb&&(x8?dx==1||!ft&&!fl:ft||fl)) {fifo[m++]=px2;Data[px2]=d;};
              if(Data[(px2=px-Width+1)]==clb&&(x8?dx==1||!ft&&!fr:ft||fr)) {fifo[m++]=px2;Data[px2]=d;};
              if(Data[(px2=px+Width-1)]==clb&&(x8?dx==1||!fb&&!fl:fb||fl)) {fifo[m++]=px2;Data[px2]=d;};
              if(Data[(px2=px+Width+1)]==clb&&(x8?dx==1||!fb&&!fr:fb||fr)) {fifo[m++]=px2;Data[px2]=d;};              
              if(dx!=1) d++;
            }            
          }
        }
        d=-d;
        int d1=d-1,d0=0,dd=0;
        bool lo=false,hi=false;
        if(border==1) {
          int dn=-Data[y*Width+x];
          if(limit>0&&limit<dn) dn=limit;
          hi=true;dd=d1=dn;
        } else if(limit>0&&d1>limit) {
          if(border>0) {
            hi=true;dd=limit;d1=limit;
          } else {           
            lo=true;d0=d-limit;d1=limit;
          }
        }
        for(n=0;n<m;n++) {
          px=fifo[n];
          int d2=-Data[px]-d0-1;
          if(lo&&d2<dd||hi&&d2>dd) { //border>0?border==1?d2>d:limit>0&&d2<limit:limit>0&&d2<d-limit) {
            Data[px]=undo!=null?undo.Data[px]:clr;
            continue;
          }
          Data[px]=Palette.RGBMix(color1,color2,d2,d1,gammax);
        }
        return m;      
      }

      public int Fill4Fifo2(int m,int[] fifo,bool fill2black,int clr,int x8,int limit,int r,int d,int b,int dx) {
        int r2=r,x,l,b2=(~15)^b;
        bool bb;
        for(l=limit;(l--)>0;) {
          x=Data[r+=dx];
          if((bb=fill2black?x>0:x==clr)||(x^b2)==d) {
            if(bb) Data[fifo[m++]=r]=d|b;
          } else break;
        }
        for(r=r2,l=limit;(l--)>0;) {
          x=Data[r-=dx];
          if((bb=fill2black?x>0:x==clr)||(x^b2)==d) {
            if(bb) Data[fifo[m++]=r]=d|b;
          } else break;
        }
        return m;
      }
      public int Fill4Fifo4(int m,int[] fifo,bool fill2black,int clr,int x4,int limit,int r,int d) {
        int i=1,b=1;
        for(i=0;i<4;i++,b<<=1)
          if(0!=(x4&b)) m=Fill4Fifo2(m,fifo,fill2black,clr,x4,limit,r,d,b,(i!=0?Width:0)+((i&1)!=0?i==3?-1:0:1));
        return m;
      }
      public int FifoFill4(int m,int[] fifo,bool fill2black,int clr,bool d8,int x4,int limit) {
        int n,m2,j,r,s,d=-16,x;
        bool bb;
        if(limit<1) limit=1<<16;
        for(n=0;n<m;n++) Data[fifo[n]]=d;
        for(n=0,m2=m;n<m2;n++)
          m=Fill4Fifo4(m,fifo,fill2black,clr,x4,limit,fifo[n],d);
        for(n=0;n<m;n++) {
          r=fifo[n];
          d=Data[r]-16;
          for(j=0;j<2;j++) {
            x=Data[s=r-((j&1)!=0?-1:1)]; 
            if((bb=fill2black?x>0:x==clr)||(x&~15)==d) {
              if(bb) Data[fifo[m++]=s]=d|x4;
              m=Fill4Fifo4(m,fifo,fill2black,clr,x4,limit,s,d);
            }
            x=Data[s=r-((j&1)!=0?-Width:Width)];
            if((bb=fill2black?x>0:x==clr)||x==d) {
              if(bb) Data[fifo[m++]=s]=d|x4;
              m=Fill4Fifo4(m,fifo,fill2black,clr,x4,limit,s,d);
            }
          }
          if(d8) {
            for(j=0;j<4;j++) {
              x=Data[s=r+((j&1)!=0?-1:1)+((j&2)!=0?-Width:Width)];
              if((bb=fill2black?x>0:x==clr)||(x&~15)==d) {
                if(bb) Data[fifo[m++]=s]=d|x4;
                m=Fill4Fifo4(m,fifo,fill2black,clr,x4,limit,s,d);
              }
            }
          }
        }
        for(n=0;n<m;) Data[fifo[n++]]>>=4;
        return m;
      }
      public int FloodFill4(int[] xy,int color1,int color2,bool noblack,bool fill2black,bool d8,int x4,int gammax) {
        int x=xy[0],y=xy[1];
        if(x<1||x>=Width-1||y<1||y>=Height-1) return 0;
        int clr=Data[y*Width+x];
        if(noblack&&(clr&0xffffff)==0) return 0;
        Border(fill2black?0:0x7fffffff);
        int m;
        int[] fifo=CreateFifo(xy,-16,noblack,out m);
        m=FifoFill4(m,fifo,fill2black,clr,d8,x4,0);
        FifoColor(m,fifo,color1,color2,-1,Data[fifo[m-1]],gammax);
        return m;
      }

      public int FifoFill1(int m,int[] fifo,bool fill2black,int clr,bool d8) {
        int n=0,r,s,d=-1,x;
        while(n<m) {
          d=Data[r=fifo[n++]];
          x=Data[s=r+1];if(fill2black?x>0:x==clr) Data[fifo[m++]=s]=d;
          x=Data[s=r-1];if(fill2black?x>0:x==clr) Data[fifo[m++]=s]=d;
          x=Data[s=r+Width];if(fill2black?x>0:x==clr) Data[fifo[m++]=s]=d;
          x=Data[s=r-Width];if(fill2black?x>0:x==clr) Data[fifo[m++]=s]=d;
          if(d8) {
            x=Data[s=r+Width-1];if(fill2black?x>0:x==clr) Data[fifo[m++]=s]=d;
            x=Data[s=r+Width+1];if(fill2black?x>0:x==clr) Data[fifo[m++]=s]=d;
            x=Data[s=r-Width-1];if(fill2black?x>0:x==clr) Data[fifo[m++]=s]=d;
            x=Data[s=r-Width+1];if(fill2black?x>0:x==clr) Data[fifo[m++]=s]=d;
          }
        }
        return m;
      }

      public int FifoFillGrad(int m,int[] fifo,bool fill2black,int clr,bool d8,int x3) {
        int n=0,k=0,r,s,d,d1=1,d2=1,b,x;
        if(x3==4) {d1=2;d2=-1;}
        else if(x3==1) d2=0;else if(x3==3) d1=2;
        while(n<m) {
          d=Data[r=fifo[n++]]-d1;
          b=0;
          if(d2<0) {
            d+=d2;
            x=Data[s=r+1];if(fill2black?x>0:x==clr) {b=1;fifo[m++]=fifo[k];Data[fifo[k++]=s]=d;}
            x=Data[s=r-1];if(fill2black?x>0:x==clr) {b|=2;fifo[m++]=fifo[k];Data[fifo[k++]=s]=d;}
            x=Data[s=r+Width];if(fill2black?x>0:x==clr) {b|=4;fifo[m++]=fifo[k];Data[fifo[k++]=s]=d;}
            x=Data[s=r-Width];if(fill2black?x>0:x==clr) {b|=8;fifo[m++]=fifo[k];Data[fifo[k++]=s]=d;}
            d-=d2;
            if(k<n) k=n;
            while(k<m&&Data[fifo[k]]>=d) k++;
            if(d8||0!=(b&6)) { x=Data[s=r+Width-1];if(fill2black?x>0:x==clr) {fifo[m++]=fifo[k];Data[fifo[k++]=s]=d;}}
            if(d8||0!=(b&5)) { x=Data[s=r+Width+1];if(fill2black?x>0:x==clr) {fifo[m++]=fifo[k];Data[fifo[k++]=s]=d;}}
            if(d8||0!=(b&10)) { x=Data[s=r-Width-1];if(fill2black?x>0:x==clr) {fifo[m++]=fifo[k];Data[fifo[k++]=s]=d;}}
            if(d8||0!=(b&9)) { x=Data[s=r-Width+1];if(fill2black?x>0:x==clr) {fifo[m++]=fifo[k];Data[fifo[k++]=s]=d;}}
          } else {
            if(d2>0) {
              if(k<n) k=n;
              while(k<m&&Data[fifo[k]]>=d) k++;
              x=Data[s=r+1];if(fill2black?x>0:x==clr) {b=1;fifo[m++]=fifo[k];Data[fifo[k++]=s]=d;}
              x=Data[s=r-1];if(fill2black?x>0:x==clr) {b|=2;fifo[m++]=fifo[k];Data[fifo[k++]=s]=d;}
              x=Data[s=r+Width];if(fill2black?x>0:x==clr) {b|=4;fifo[m++]=fifo[k];Data[fifo[k++]=s]=d;}
              x=Data[s=r-Width];if(fill2black?x>0:x==clr) {b|=8;fifo[m++]=fifo[k];Data[fifo[k++]=s]=d;}
              d-=d2;
            } else {
              x=Data[s=r+1];if(fill2black?x>0:x==clr) {b=1;Data[fifo[m++]=s]=d;}
              x=Data[s=r-1];if(fill2black?x>0:x==clr) {b|=2;Data[fifo[m++]=s]=d;}
              x=Data[s=r+Width];if(fill2black?x>0:x==clr) {b|=4;Data[fifo[m++]=s]=d;}
              x=Data[s=r-Width];if(fill2black?x>0:x==clr) {b|=8;Data[fifo[m++]=s]=d;}
            }
            if(d8||0!=(b&6)) { x=Data[s=r+Width-1];if(fill2black?x>0:x==clr) Data[fifo[m++]=s]=d;}
            if(d8||0!=(b&5)) { x=Data[s=r+Width+1];if(fill2black?x>0:x==clr) Data[fifo[m++]=s]=d;}
            if(d8||0!=(b&10)) { x=Data[s=r-Width-1];if(fill2black?x>0:x==clr) Data[fifo[m++]=s]=d;}
            if(d8||0!=(b&9)) { x=Data[s=r-Width+1];if(fill2black?x>0:x==clr) Data[fifo[m++]=s]=d;}
          }
        }
        return m;
      }

      public int FloodVector(int[] xy,int color1,int color2,bool noblack,bool fill2black,bool d8,int dx,int dy,bool sharp,int gammax) {
        int x=xy[0],y=xy[1],l;
        if(x<1||x>=Width-1||y<1||y>=Height-1) return 0;
        int clr=Data[y*Width+x];
        if(noblack&&(clr&0xffffff)==0) return 0;
        Border(fill2black?0:0x7fffffff);
        int m;
        int[] fifo=CreateFifo(xy,-1,noblack,out m);
        m=FifoFill1(m,fifo,fill2black,clr,d8);
        l=xy.Length;
        FifoColorVect(m,fifo,color1,color2,new int[] {xy[l-2],xy[l-1]},dx,dy,sharp,gammax);
        return m;
      }
      public fillres FloodFloat(int[] xy,int color1,int color2,bool noblack,bool fill2black,bool d8,PointPath gxy,int gx) {
        int x=xy[0],y=xy[1],l;
        fillres fr=new fillres();
        if(x<1||x>=Width-1||y<1||y>=Height-1||gxy.Count<1) return fr;
        int clr=Data[y*Width+x];
        if(noblack&&(clr&0xffffff)==0) return fr;
        Border(fill2black?0:0x7fffffff);
        int n,m,d,i,i2,j;
        int[] fifo=CreateFifo(xy,-1,noblack,out m);
        m=FifoFill1(m,fifo,fill2black,clr,d8);
        l=xy.Length;
        for(n=0;n<m;n++) {
          d=fifo[n];fifo[n]=((y=d/Width)<<16)|(x=d%Width);
          fr.Add(x,y);
        }
        float[] sa=new float[m],fa=new float[m];
        double e,f,fi,fm,si=0,si2;
        for(n=0;n<m;n++) sa[n]=1;
        for(i=0;i<gxy.Count;i=i2) {
          for(i2=i+1;i2<gxy.Count&&!gxy[i2-1].stop;i2++);
          fi=float.PositiveInfinity;fm=float.NegativeInfinity;
          for(n=0;n<m;n++) {
            d=fifo[n];x=d&0xffff;y=d>>16;
            f=float.PositiveInfinity;
            PathPoint pp2,pp=gxy[gxy.Count-1];
            pp.stop=!gxy.Closed;
            for(j=i;j<i2;j++) {
              pp2=pp;pp=gxy[j];
              if(pp.shape!=0) continue;
              e=(float)(pp2.stop?sqr2d(x-pp.x,y-pp.y)                 
                :pp2.shape==2?sqr2b(x,y,pp.x,pp.y,pp2.x,pp2.y,pp2.fill)
                :pp2.shape==1?sqr2c(x,y,pp.x,pp.y,pp2.x,pp2.y,pp2.fill)
                :pp2.shape==4?sqr2d(x,y,pp.x,pp.y,pp2.x,pp2.y,pp2.fill)
                :pp2.shape==3?sqr2t(x,y,pp2.x,pp2.y,pp.x,pp.y,pp2.fill)
                :pp2.shape==6?sqr2h(x,y,pp2.x,pp2.y,pp.x,pp.y,pp2.fill)
                :sqr2l(x,y,pp.x,pp.y,pp2.x,pp2.y));
              if(e<f) f=e;
            }
            fa[n]=(float)f;
            if(f<fi) fi=f;else if(f>fm) fm=f;
          }
          if(fa[0]>fm) fm=fa[0];
          bool sqrt=true;
          fi=Math.Sqrt(fi);fm=Math.Sqrt(fm);
          if(sqrt) {
            fi=(Math.Sqrt(fi));
            fm=(Math.Sqrt(fm));
          }
          fm=1/(fm-fi);
          si2=si>0?1/si:1;si=0;
          for(n=0;n<m;n++) {
            f=Math.Sqrt(fa[n]);
            if(sqrt) f=Math.Sqrt(f);
            f=(f-fi)*fm;
            e=f*sa[n]*si2;
            sa[n]=(float)e;
            if(e>si) si=e;
          }
        }
        si2=si>0?1/si:1;
        for(n=0;n<m;n++) {
          d=fifo[n];x=d&0xffff;y=d>>16;
          f=sa[n]*si2;
          if(gx!=0) f=Palette.Gammax(gx,f);
          Data[y*Width+x]=Palette.RGBMix8(color1,color2,(int)(256.75*f));
        }
        return fr;
      }  

      public static double sqr2d(double x,double y) { return x*x+y*y;}
      public static double sqr2l(double x,double y,double x0,double y0,double x1,double y1) {
        double dx=x1-x0,dy=y1-y0,d=dx*dx+dy*dy,ax=x-x0,ay=y-y0,e=ax*dx+ay*dy;
        if(e<=0) return ax*ax+ay*ay;
        if(e>=d) { ax=x-x1;ay=y-y1;return ax*ax+ay*ay;}
        e=ax*dy-ay*dx;
        return e*e/d;
      }
      public static double sqr2c(double x,double y,double x0,double y0,double x1,double y1,bool fill) {
        double cx=(x0+x1)/2,cy=(y0+y1)/2,ax=x-cx,ay=y-cy,d=ax*ax+ay*ay,r2;
        ax=x0-cx;ay=y0-cy;r2=ax*ax+ay*ay;
        if(fill&&d<=r2) return 0;
        d=Math.Abs(Math.Sqrt(d)-Math.Sqrt(r2));
        return d*d;
      }
      public static double sqr2b(double x,double y,double x0,double y0,double x1,double y1,bool fill) {
        double d,nx,ny;
        if(x0>x1) {d=x0;x0=x1;x1=d;}
        if(y0>y1) {d=y0;y0=y1;y1=d;}
        nx=x<x0?x0:x>x1?x1:x;
        ny=y<y0?y0:y>y1?y1:y;
        if(nx==x&&ny==y) {
          if(fill) return 0;
          nx=2*x<x0+x1?x0:x1;ny=2*y<y0+y1?y0:y1;
          d=Math.Min(Math.Abs(x-nx),Math.Abs(y-ny));
        } else if(nx!=x&&ny!=y)
          return sqr2d(x-nx,y-ny);
        else 
          d=Math.Max(Math.Abs(x-nx),Math.Abs(y-ny));
        return d*d;
      }
      public static double sqr2d(double x,double y,double x0,double y0,double x1,double y1,bool fill) {
        double dx=x1-x0,dy=y1-y0,ax=2*x-x0-x1,ay=2*y-y0-y1,bx=dx-dy,by=dx+dy,a=ax*bx+ay*by,b=ax*by-ay*bx,d=bx*bx+by*by,e=Math.Abs(a),f=Math.Abs(b),g=Math.Max(e,f),h=bx*bx+by*by;
        g=(g-h/2);
        if(g<0) d=fill?0:g*g/h;
        else {           
          if(e<h/2||f<h/2) d=g*g/h;else { ax=e-h/2;ay=f-h/2;d=(ax*ax+ay*ay)/h;}
        }
        return d;
      }
      public static bool sqr2lx(double x,double y,double x0,double y0,double x1,double y1,ref double min) {
        double dx=x1-x0,dy=y1-y0,d=dx*dx+dy*dy,ax=x-x0,ay=y-y0,e=ax*dx+ay*dy,n=ax*dy-ay*dx;
        if(e<=0) return n>=0;
        if(e>=d) { ax=x-x1;ay=y-y1;d=ax*ax+ay*ay;}
        else {
          e=ax*dy-ay*dx;
          d=e*e/d;
        }
        if(d<min) min=d;
        return n>=0;
      }
      public static double sqr2t(double x,double y,double x0,double y0,double x1,double y1,bool fill) {
        double min=double.PositiveInfinity,dx=x1-x0,dy=y1-y0,x2=x1-dy/2,y2=y1+dx/2,x3=x1+dy/2,y3=y1-dx/2;
        bool inside=sqr2lx(x,y,x0,y0,x2,y2,ref min); 
        inside&=sqr2lx(x,y,x2,y2,x3,y3,ref min); 
        inside&=sqr2lx(x,y,x3,y3,x0,y0,ref min); 
        return fill&&inside?0:min;
      }
      static readonly double sq32=Math.Sqrt(3.0)/4;
      public static double sqr2h(double x,double y,double x0,double y0,double x1,double y1,bool fill) {
        double min=double.PositiveInfinity,dx=x1-x0,dy=y1-y0,x2=x0+dx/4,y2=y0+dy/4,x3=x1-dx/4,y3=y1-dy/4,ax,ay,bx,by;         
        bool inside=sqr2lx(x,y,x0,y0,ax=x2-dy*sq32,ay=y2+dx*sq32,ref min); 
        inside&=sqr2lx(x,y,ax,ay,ax=x3-dy*sq32,ay=y3+dx*sq32,ref min); 
        inside&=sqr2lx(x,y,ax,ay,ax=x1,ay=y1,ref min); 
        inside&=sqr2lx(x,y,ax,ay,ax=x3+dy*sq32,ay=y3-dx*sq32,ref min); 
        inside&=sqr2lx(x,y,ax,ay,ax=x2+dy*sq32,ay=y2-dx*sq32,ref min); 
        inside&=sqr2lx(x,y,ax,ay,x0,y0,ref min); 
        return fill&&inside?0:min;
      }


      public static double mind(double x,double y) { return x<y?x:y;}
      public static double maxd(double x,double y) { return x>y?x:y;}
      public static double absd(double x) { return x<0?-x:x;}
    public static double squarle2(double x,double y,double r) {
      double x2,y2,c,d,r2=r*r;
      x-=0.5;y-=0.5;
      d=(x2=x*x)+(y2=y*y);
      if(d<=r2) return d/0.5; 
      if(x2<y2) {c=x2;x2=y2;y2=c;}
      d=(d-r2)/(y2/x2+0.25-r2);
      return (r2+d*(0.5-r2))/0.5;
    }
    public static double sqrt2=Math.Sqrt(2);
    public static double squarle(double x,double y,double r) {
      double x2,y2,c,d,r2=r*r;
      x-=0.5;y-=0.5;
      d=(x2=x*x)+(y2=y*y);
      if(d<=r2) return Math.Sqrt(d)/sqrt2*2; 
      if(x<0) x=-x;if(y<0) y=-y;if(x<y) {c=x;x=y;y=c;}
      d=Math.Sqrt(d);c=r*x/d;d=(x-c)/(0.5-c);
      return (r+d*(sqrt2/2-r))/sqrt2*2;
    }
    public static double squarlet(double x,double y,double r,double cx,double cy,double cxy) {
      double dx,dy,a,b,c,d,r2=r*r;
      d=x*x+y*y;
      if(d<=r2) return Math.Sqrt(d)/cxy;
      dx=x-cx;dy=y-cy;
      if(dx>=0||dy>=0) return 1;
      a=dx*dx+dy*dy;b=2*cx*dx+2*cy*dy;c=cx*cx+cy*cy-r2;
      c=b*b-4*a*c;
      if(c>0) {
        d=(-b-Math.Sqrt(c))/2/a;
        dx=cx+d*dx;dy=cy+d*dy;
      } else dy=-1;
      d=dy<0?(x-r)/(cx-r):(y-dy)/(cy-dy);
      c=r+d*(cxy-r);
      return c>=cxy?1:c/cxy;
    }

    public static double squarle3(double x,double y,double r) {
      double x2,y2,dx,dy,a,b,c,d,r2=r*r;
      x-=0.5;y-=0.5;
      d=(x2=x*x)+(y2=y*y);
      if(d<=r2) return Math.Sqrt(d)/sqrt2*2; 
      if(x<0) x=-x;if(y<0) y=-y;if(x<y) {c=x;x=y;y=c;}
      dx=x-0.5;dy=y-0.5;
      a=dx*dx+dy*dy;b=dx+dy;c=0.5-r2;
      c=b*b-4*a*c;
      if(c>0) {
        d=(-b-Math.Sqrt(c))/2/a;
        dx=0.5+d*dx;dy=0.5+d*dy;
      } else dy=-1;
      d=dy<0?(x-r)/(0.5-r):(y-dy)/(0.5-dy);
      return (r+d*(sqrt2/2-r))/sqrt2*2;
    }

      public int FloodSquare(int sx,int sy,int mx,int my,bool mir,int color1,int color2,bool noblack,bool fill2black,bool d8,int mode,int gammax) {
        int x=sx,y=sy,shape=mode>>4;
        if(x<1||x>=Width-1||y<1||y>=Height-1) return 0;
        int clr=Data[y*Width+x];
        if(noblack&&(clr&0xffffff)==0) return 0;
        mode&=15;
        Border(fill2black?0:0x7fffffff);
        int m,dx=mx-sx,dy=my-sy,c,ax=-sy*dy-sx*dx,ay=-sy*dx+sx*dy,bx,by;        
        double d=dx*dx+dy*dy,rx,ry,fx,fy,yr;
        double q3=Math.Sqrt(3)/2,q3i=1/q3,q2=9/4/q3/q3,cx,cy;
        if(d<1) return 0;
        int[] fifo=CreateFifo(new int[] {x,y},-1,noblack,out m);
        m=FifoFill1(m,fifo,fill2black,clr,d8);
        for(int i=0,r;i<m;i++) {
          r=fifo[i];y=Math.DivRem(r,Width,out x);
          bx=ax+dy*y+dx*x;by=ay+dx*y-dy*x;
          if(shape==4) {
      	    rx=bx/d;ry=by/d;rx-=fx=Math.Floor(rx);ry-=fy=Math.Floor(ry);
            if(0!=(((int)fy)&1)) ry=1-ry;if(0!=(((int)fx)&1)) rx=1-rx;
            if(rx+ry>1) {rx=1-rx;ry=1-ry;}
            if(mode!=0) {rx-=0.25;ry-=0.25;rx=maxd(maxd(-ry/0.25,-rx/0.25),(rx+ry)/0.5);}
            else rx=sqr2d(rx-0.25,ry-0.25)/Math.Sqrt(sqr2d(0.25,0.75));
          } else if(shape==3) {
	          ry=by/d*2/3;ry-=fy=Math.Floor(ry);ry*=1.5;rx=bx/d*q3i/2;rx+=0!=(((int)fy)&1)?0.5:0;rx-=fx=Math.Floor(rx);rx*=2*q3;
            if(ry>1+rx*0.5*q3i) {ry-=1.5;rx+=q3;}
            else if(ry>2-rx*0.5*q3i) {ry-=1.5;rx-=q3;}
            if(mode==3) rx=rx/q3/2;
            else if(mode==4) {
              ry+=0.5;rx-=q3;rx=mind(q3*ry+0.5*rx,q3*ry-0.5*rx)/q3/2;
            } else if(mode==2) {
              ry+=0.5;rx-=q3;fx=mind(q3*ry+0.5*rx,q3*ry-0.5*rx);
              fx=mind(mind(fx,q3-rx),q3+rx);
              rx=1-fx/q3;
            } else if(mode==1) {
              rx-=q3;ry-=0.5;rx=absd(rx);ry=absd(ry);
              if(q3*ry>0.5*rx) {fx=0.5*rx+q3*ry;ry=-q3*rx+0.5*ry;rx=fx;ry=absd(ry);}
              rx=squarlet(rx,ry,0.75,q3,0.5,1);
            } else if(mode==6) {
              rx-=q3;ry-=0.5;
              if(ry<rx*q3i/2&&ry<-rx*q3i/2) rx=(rx+q3)/2*q3i/3;
              else rx=((rx<0?1:2)+(ry+0.5)/1.5)/3;
            } else if(mode!=0) {
              rx-=q3;ry-=0.5;
              fx=maxd((-rx)*q3i,rx*q3i);
              fx=maxd(fx,maxd((-rx/2+ry*q3)*q3i,(rx/2+ry*q3)*q3i));
              rx=maxd(fx,maxd((-rx/2-ry*q3)*q3i,(rx/2-ry*q3)*q3i));
            } else rx=sqr2d(rx-q3,ry-0.5);
          } else if(shape==2) {
 	          rx=bx/d;ry=by/d*q3i;ry-=fy=Math.Floor(ry);ry*=q3;rx+=0!=(((int)fy)&1)?0.5:0;rx-=fx=Math.Floor(rx);
            if(ry>2*q3*rx) {rx+=0.5;ry=q3-ry;}
            else if(ry>2*q3*(1-rx)) {rx-=0.5;ry=q3-ry;}
            cx=0.5;cy=q3/3;
          //if(mode) rx=maxf(maxf((-2*q3*rx+ry+q3*2/3)*3/2*q3i,(2*q3*rx+ry-2*q3+q3*2/3)*3/2*q3i),(q3/3-ry)*3*q3i);
            if(mode==1) {
              rx-=cx;ry-=cy;rx=absd(rx);
              if(q3*ry>-0.5*rx) {fx=-0.5*rx+q3*ry;ry=-q3*rx-0.5*ry;rx=absd(fx);}
              rx=squarlet(-ry,rx,0.25,q3/3,0.5,q3*2/3);
            } else if(mode!=0) {rx-=cx;ry-=cy;rx=maxd(maxd((-2*q3*rx+ry)*3/2*q3i,(2*q3*rx+ry)*3/2*q3i),(-ry)*3*q3i);}
            else rx=sqr2d(rx-cx,ry-cy)*q2;
          } else {
  	        rx=bx/d;ry=by/d;ry-=fy=Math.Floor(ry);
            if(shape==1) rx+=0!=(((int)fy)&1)?0.5:0;
            rx-=fx=Math.Floor(rx);
	          if(mode==2) {
              rx-=0.5;ry=0.5-ry;
              if(rx<0) {rx+=0.5;ry+=0.5*(ry<0?1:-1);}
              rx=(sqr2d(ry<0?0.5-rx:rx,ry<0?0.5+ry:ry)<0.25)==(ry<0)?rx:0.5+rx;
              //rx=rx*rx+ry*ry<0.25?0.5+ry:ry;
              //rx=(rx*rx+ry*ry<0.25?sqr2f(0.5+ry,rx):sqr2f(rx-0.5,ry));
              //rx=4*(rx*rx+ry*ry<0.25?sqr2f(ry,rx):sqr2f(rx-0.5,ry-0.5));
            } else if(mode==7) rx=maxd(absd(1-rx),absd(ry));
            else if(mode==12) {
              if(rx>0.5) {
                if(sqr2d(ry-0.5,rx-0.5)>0.25) {rx-=0.5;ry+=0.5;ry-=Math.Floor(ry);}
              } else {
                if(sqr2d(ry-(ry<0.5?0:1),rx)<0.25) {rx+=0.5;ry+=0.5;ry-=Math.Floor(ry);}
              }
              rx=1-rx;
            } else if(mode==11) {rx=2*rx;rx-=Math.Floor(rx);ry-=0.5;rx-=2*Math.Sqrt(0.36-ry*ry);rx-=Math.Floor(rx);}
            else if(mode==10) {rx=2*rx;rx-=Math.Floor(rx);rx-=Math.Sin(ry*2*Math.PI)/2;rx-=Math.Floor(rx);}
            else if(mode==9) {rx=2*rx;rx-=Math.Floor(rx);rx-=Math.Sin(2*ry*2*Math.PI)/4;rx-=Math.Floor(rx);}
            else if(mode==8) { rx=2*rx;rx-=Math.Floor(rx);rx-=2*Math.Max(Math.Abs(ry-0.25),Math.Abs(ry-0.75));rx-=Math.Floor(rx);}
            else if(mode==6) rx=squarle2(rx,ry,0.375);
            else if(mode==5) rx=squarle3(rx,ry,0.375);
	          else if(mode==4) rx=2*maxd(absd(rx-0.5),absd(ry-0.5));
	          else if(mode==3) rx=(rx+ry)/2;
	          else if(mode==2) rx=ry;
	          else if(mode==1) rx=rx;
	          else {rx-=0.5;ry-=0.5;rx=2*(rx*rx+ry*ry);}
            if(shape==5) rx=(((int)fx)&1)==(((int)fy)&1)?rx/2:mir?0.5+rx/2:1-rx/2;
          }
          if(mir) rx=rx>0.5?2*(1-rx):2*rx;
          if(gammax!=0) rx=Palette.Gammax(gammax,rx);
          c=Palette.RGBMix8(color1,color2,(int)(rx*256.875));
          Data[r]=c;
        } 
        return m;
      }                    

      public int Search(bool d8,int limit,int s,IntCmp.Func found,out int r) {
        int i,j,dx,dy;
        if(limit<1) limit=1<<16;
        for(i=1;i<=limit;i++) {
          for(j=0,dx=0,dy=-i;j<4;j++,r=dx,dx=-dy,dy=r) {
            if(found(Data[r=s+Width*dy+dx])) return 2*j;
            if(d8) if(found(Data[r=s+Width*(dy+dx)+dx-dy])) return 2*j+1;
          }
        }
        r=-1;
        return -1;
      }
      public int FifoBorder(int m,int[] fifo,bool d8) {
        int n=0,g=0,d,r;
        for(;n<m;n++) {
          r=fifo[n];
          bool e=Data[r+1]>=0||Data[r-1]>=0||Data[r-Width]>=0||Data[r+Width]>=0;
          if(!e&&d8) e=Data[r-Width-1]>=0||Data[r-Width+1]>=0||Data[r+Width-1]>=0||Data[r+Width+1]>=0;
          if(e) { if(g<n) { fifo[n]=fifo[g];fifo[g]=r;};g++;}
        }
        return g;
      }

      public int FifoOutline(int m,int[] fifo,int r,bool d8,IntCmp.Func border,int c) {
        int g,h,d,h3,j,w2=Width;
        d=Search(false,0,r,border,out g);
        if(g==0) return m;
        d/=2;
        if(d==1) h=g-1;
        else if(d==3) h=g+1;
        else if(d==2) h=g-w2;
        else h=g+w2;
        g=h;
        do {
          if(Data[h]!=c) fifo[m++]=h;
          Data[h]=c;
          for(j=0;j<4;j++,d=(d+1)&3) {
            if(!border(Data[h3=h+(d==0?-w2:d==1?1:d==2?+w2:-1)])) {
              if(d8) {
                int h4=h3+(d==0?-1:d==1?-w2:d==2?+1:+w2);
                if(!border(Data[h4])) {
                  h3=h4;
                  d=(d+3)&3;
                }
              }
              h=h3;
              d=(d+3)&3;
              break;
            }
          }
        } while(h!=g);
        return m;
      }

      public static int[] Clone(int[] src,int n,int m) {
        int[] x=new int[m-=n];
        Array.Copy(src,n,x,0,m);
        return x;
      }
      public int FifoRemove(int m0,int m,int[] fifo,IntCmp.Func remove) {
        int n=m0,g=n,r;
        for(;n<m;n++)
          if(!remove(Data[r=fifo[n]])) {
            if(g<n) fifo[g]=r;
            g++;
          }
        return g;
      }

      public int FloodBorder(int[] xy,int color1,int color2,bool noblack,bool fill2black,bool d8,int x3,int gammax) {
        int x=xy[0],y=xy[1],l;
        if(x<1||x>=Width-1||y<1||y>=Height-1) return 0;
        int clr=Data[y*Width+x];
        if(noblack&&(clr&0xffffff)==0) return 0;
        Border(fill2black?0:0x7fffffff);
        int m,e,m0,be=0;
        int[] fifo=CreateFifo(xy,-1,noblack,out m0),bord=null;
        m=FifoFill1(m0,fifo,fill2black,clr,d8);
        int cmax;
        if(m0<2) {
          e=FifoBorder(m,fifo,d8);
          FifoOp(0,e,fifo,null,-1);
          FifoOp(e,m,fifo,null,clr);
          m=FifoFillGrad(e,fifo,false,clr,d8,x3);
          cmax=Data[fifo[m-1]];
        } else {
          xy=Clone(fifo,0,m0);
          bord=Clone(fifo,0,be=FifoBorder(m,fifo,d8));
          FifoOp(0,m,fifo,null,clr);
          e=0;
          IntCmp.Func border=new IntCmp(clr).NEP;
          foreach(int f in xy)
            e=FifoOutline(e,fifo,f,d8,border,-1);
          be=FifoRemove(0,be,bord,new IntCmp(-1).EQ);
          m=FifoFillGrad(e,fifo,false,clr,d8,x3);
          cmax=Data[fifo[m-1]];
          if(be>0) {
            bmap b2=new bmap(this);
            FifoOp(0,m,fifo,null,clr);
            Array.Copy(bord,fifo,be);
            FifoOp(0,be,fifo,null,-1);
            m=FifoFillGrad(be,fifo,false,clr,d8,x3);
            FifoOp(0,m,fifo,b2,6,out cmax);
          }
        }        
          
        FifoColor(m,fifo,color1,color2,0,cmax,gammax);
        return m;
      }

      static void MinMax(int x,ref int min,ref int max) {
        if(x<min) min=x;else if(x>max) max=x;
      }
      static void MinMax(int x,ref int max) { if(x>max) max=x; }

      public int FifoOp(int m0,int m,int[] fifo,bmap src,int op) { int max;return FifoOp(m0,m,fifo,src,op,out max);}
      public int FifoOp(int m0,int m,int[] fifo,bmap src,int op,out int max) {
        int i,a,b,r,min=int.MinValue;
        max=int.MaxValue;
        for(i=m0;i<m;i++) {
          r=fifo[i];
          if(src==null) a=op;
          else {
            a=Data[r];b=src.Data[r];
            switch(op) {
             case 1:a=b;break;
             case 2:if(b<a) a=b;break;
             case 3:if(b>a) a=b;break;
             case 4:a=a+b;break;
             case 5:a=(a+b)/2;break;
             case 6:
             case 7:
               a+=1;b+=1;
               if(op==7) {a=isqrt(-a<<8);b=isqrt(-b<<8);}
               a=a!=0?b!=0?a*255/(a+b):255:b!=0?0:128;
               a=-a-1;
               break;           
            }
          }
          if(a>min) { min=a;if(i==m0) max=a;} else if(a<max) max=a;
          Data[r]=a;
        }
        return min;
      }

      public void FifoColor(int m,int[] fifo,int color) {
        for(int i=0;i<m;i++)
          Data[fifo[i]]=color;
      }
      public void FifoColor(int m,int[] fifo,int color1,int color2,int min,int max,int gammax) {
        max=min-max;
        for(int n=0,px,d;n<m;n++) {
          px=fifo[n];
          d=-Data[px]-min-1;
          Data[px]=Palette.RGBMix(color1,color2,d,max,gammax);
        }
      }
      public void FifoColorVect(int m,int[] fifo,int color1,int color2,int[] xy,int dx,int dy,bool sharp,int gammax) {
        int min=int.MaxValue,max=int.MinValue,x,y,s,r,c=0,i,j;
        int[] a=new int[xy.Length/2+2];
        for(i=0;i<m;i++) {
          r=fifo[i];
          y=Math.DivRem(r,Width,out x);
          s=dx*x+dy*y;
          if(s<min) {
            min=s;
            if(i==0) max=s;
          } else if(s>max) max=s;
        }
        a[c++]=min;
        for(j=0;j<xy.Length;j+=2) {
          x=xy[j];y=xy[j+1];
          s=dx*x+dy*y;
          if(s>=min&&s<=max) a[c++]=s;
        }
        a[c++]=max;
        Array.Resize(ref a,c);
        Array.Sort(a);      
        if(a.Length==1) max=0;
        else {
          max=2*(a[1]-a[0]);
          for(j=3;j<c;j++) 
            MinMax(a[j-1]-a[j-2],ref max);
          MinMax(2*(a[c-1]-a[c-2]),ref max);
        }
        min=max;
        for(i=0;i<m;i++) {
          r=fifo[i];
          y=Math.DivRem(r,Width,out x);
          s=dx*x+dy*y;
          if(s<=a[1]) {s=2*(a[1]-s);max=2*(a[1]-a[0]);}
          else if(s>=a[c-2]) {s=2*(s-a[c-2]);max=2*(a[c-1]-a[c-2]);}
          else 
            for(j=2;j+1<c;j++)
               if(s<=a[j]) {
                 max=a[j]-a[j-1];
                 if(2*s<a[j]+a[j-1]) {
                   s=2*(s-a[j-1]);
                 } else {
                   s=2*(a[j]-s);
                 }
                 break;
               }
          if(!sharp) max=min;
          Data[r]=Palette.RGBMix(color1,color2,s,max,gammax);
        }  
      }

      internal void Expand(bool x8,bool wonly,bmap src,int[] rect,int color,int incolor,int bcolor,int outcolor) {
			  if(!R.Intersect(rect,1,1,Width-1,Height-1)) return;
        if(src==null) src=Clone();
        src.Border(White);
        src.ClearByte();
        if(incolor==-1&&bcolor==-1&&outcolor==-1) return;
				int w=Width-2,h=Height-2;
				if(rect!=null) {w=rect[2]-rect[0]+1;h=rect[3]-rect[1]+1;}
				int i=rect!=null?rect[0]+Width*(rect[1]):Width+1;
        color&=White;
        for(int y=0;y<h;y++,i+=Width-w)
				  for(int e=i+w;i<e;i++) if(src.Data[i]==color) {
          if(incolor!=-1&&incolor!=color||bcolor!=-1&&bcolor!=color) {
            bool inner=src.Data[i-1]==color&&src.Data[i+1]==color&&src.Data[i-Width]==color&&src.Data[i+Width]==color;
            if(inner&&x8&&(src.Data[i-Width-1]!=color||src.Data[i-Width+1]!=color||src.Data[i+Width-1]!=color||src.Data[i+Width+1]!=color)) inner=false;
            if(inner&&incolor!=-1) Data[i]=incolor;
            if(!inner&&bcolor!=-1) Data[i]=bcolor;
          }
          if(outcolor!=-1) {
            if(wonly) {
              if(Data[i-1]==White) Data[i-1]=outcolor;
              if(Data[i+1]==White) Data[i+1]=outcolor;
              if(Data[i-Width]==White) Data[i-Width]=outcolor;
              if(Data[i+Width]==White) Data[i+Width]=outcolor;
							if(x8) {
								if(Data[i-Width-1]==White) Data[i-Width-1]=outcolor;
								if(Data[i-Width+1]==White) Data[i-Width+1]=outcolor;
								if(Data[i+Width-1]==White) Data[i+Width-1]=outcolor;
								if(Data[i+Width+1]==White) Data[i+Width+1]=outcolor;
							}
            } else if((incolor==outcolor&&bcolor==outcolor)||outcolor==color) {
              Data[i-1]=Data[i+1]=Data[i-Width]=Data[i+Width]=outcolor;
              if(x8) Data[i-Width-1]=Data[i-Width+1]=Data[i+Width-1]=Data[i+Width+1]=outcolor;
            } else {
              if(Data[i-1]!=color) Data[i-1]=outcolor;
              if(Data[i+1]!=color) Data[i+1]=outcolor;
              if(Data[i-Width]!=color) Data[i-Width]=outcolor;
              if(Data[i+Width]!=color) Data[i+Width]=outcolor;
							if(x8) {
								if(Data[i-Width-1]!=color) Data[i-Width-1]=outcolor;
								if(Data[i-Width+1]!=color) Data[i-Width+1]=outcolor;
								if(Data[i+Width-1]!=color) Data[i+Width-1]=outcolor;
								if(Data[i+Width+1]!=color) Data[i+Width+1]=outcolor;
							}
            }
          }
        }
      }
      internal void Impand(int[] rect,int color,int color2,byte mask,bool repeat) {
        if(color==color2) return;
			  if(!R.Intersect(rect,1,1,Width-1,Height-1)) return;
        bmap src=new bmap(this,rect[0],rect[1],rect[2],rect[3],1);
        ClearByte();
        mask&=15;
       repeat:
        for(int j=0;j<4;j++) {
          if(0==(mask&(1<<j))) continue;
          int n=0;
          src.CopyRectangle(this,rect[0],rect[1],rect[2],rect[3],1,1,-1);
          for(int y=1;y<src.Height-1;y++) {
            int h=y*src.Width+1,he=h+src.Width-2,xh=rect[0]-h;
            for(;h<he;h++) if(src.Data[h]==color) {
              bool go=false,l,r,u,d,lu,ld,ru,rd;
              l=src.Data[h-1]==color;r=src.Data[h+1]==color;u=src.Data[h-src.Width]==color;d=src.Data[h+src.Width]==color;
              lu=src.Data[h-src.Width-1]==color;ld=src.Data[h+src.Width-1]==color;
              ru=src.Data[h-src.Width+1]==color;rd=src.Data[h+src.Width+1]==color;
	            switch(j) {
	              case 0:if(!u&&d&&((ld&&l)||(rd&&r))&&!(l&&r&&!(ld||rd))&&!(lu&&!l)&&!(ru&&!r)) go=true;break;
	              case 1:if(!l&&r&&((ru&&u)||(rd&&d))&&!(u&&d&&!(ru||rd))&&!(lu&&!u)&&!(ld&&!d)) go=true;break;
	              case 2:if(!d&&u&&((lu&&l)||(ru&&r))&&!(l&&r&&!(lu||ru))&&!(ld&&!l)&&!(rd&&!r)) go=true;break;
	              case 3:if(!r&&l&&((lu&&u)||(ld&&d))&&!(u&&d&&!(lu||ld))&&!(ru&&!u)&&!(rd&&!d)) go=true;break;
	            }
              if(go) {
                n=1;
                Data[Width*(y+rect[1]-1)+h+xh]=color2;
              }
            }
          }
          if(n==0) mask&=(byte)(~(1<<j));
        }
        if(repeat&&mask!=0) {
          goto repeat;
        }
      }
      public void Roll(int dx,int dy,int x,int y,int x2,int y2) {
        R.Norm(ref x,ref y,ref x2,ref y2);
        if(x2<0||y2<0||x>=Width||y>=Height) return;
        int ww=x2-x+1,hh=y2-y+1;
        dx%=ww;dy%=hh;
        if(dx<0) dx+=ww;if(dy<0) dy+=hh;
        int ix,iy,c,d,g,h,p=y*Width+x;
        int[] buf=new int[ww>hh&&dx>0?ww:hh];
        if(dx>0) for(iy=0;iy<hh;iy++) {
          Array.Copy(Data,p+iy*Width,buf,0,ww);
          Array.Copy(buf,0,Data,p+dx+iy*Width,ww-dx);
          Array.Copy(buf,ww-dx,Data,p+iy*Width,dx);
        }        
        if(dy>0) for(ix=0;ix<ww;ix++) {
          for(h=ix,iy=0;iy<hh;iy++,h+=Width)
            buf[iy]=Data[p+h];
          for(g=ix,iy=0,h=hh-1-dy;iy<hh;iy++,g+=Width) {
            Data[p+g]=buf[h++];
            if(h==hh) h=0;
          }
        }
      }
      public void Mirror(bool vertical,bool horizontal) { 
        int x,y;
        if(vertical) {
          for(y=0;y<Height-y-1;y++) {
            int h=y*Width,g=(Height-y-1)*Width;
            for(x=0;x<Width;x++) {
              int b;
              b=Data[g];Data[g]=Data[h];Data[h]=b;
              g++;h++;
            }
          }
        } 
        if(horizontal) {
          for(x=0;x<Width-x-1;x++) {
            int h=x,g=(Width-x-1);
            for(y=0;y<Height;y++) {
              int b;
              b=Data[g];Data[g]=Data[h];Data[h]=b;
              g+=Width;h+=Width;
            }
          }
        }
      }
      public void Mirror(bool vertical,bool horizontal,int x,int y,int x2,int y2) {
        R.Norm(ref x,ref y,ref x2,ref y2);
        if(x>=Width||y>=Height||x2<0||y2<0) return;
        if(x<0) x=0;if(x2>Width-1) x2=Width-1;
        if(y<0) y=0;if(y2>Height-1) y2=Height-1;
        if(vertical) {
          for(int z=y,z2=y2;z<z2;z++,z2--) {
            int h=z*Width+x,g=z2*Width+x;
            for(int i=x;i<=x2;i++) {
              int b;
              b=Data[g];Data[g]=Data[h];Data[h]=b;
              g++;h++;
            }
          }
        }         
        if(horizontal) {
          for(;x<x2;x++,x2--) {
            int h=y*Width+x,g=y*Width+x2;
            for(int i=y;i<=y2;i++) {
              int b;
              b=Data[g];Data[g]=Data[h];Data[h]=b;
              g+=Width;h+=Width;
            }
          }
        }         
      }
      public void Insert(bool vertical,bool horizontal,int x,int y,int x2,int y2) {
        R.Norm(ref x,ref y,ref x2,ref y2);
        if(x>=Width||y>=Height||x2<0||y2<0) return;
        if(x<0) x=0;if(x2>Width-1) x2=Width-1;
        if(y<0) y=0;if(y2>Height-1) y2=Height-1;
        int h,g;
        if(vertical) {
          for(int a=Height-1;a>y;a--) {
            g=a*Width;h=(a>y2?a-y2+y-1:y)*Width;
            for(int b=0;b<Width;b++) Data[g++]=Data[h++];
          }
        }
        if(horizontal) {
          for(int a=Width-1;a>x;a--) {
            g=a;h=a>x2?a-x2+x-1:x;
            for(int b=0;b<Height;b++,g+=Width,h+=Width) Data[g]=Data[h];
          }
        }
      }
      public void Remove(bool vertical,bool horizontal,int x,int y,int x2,int y2,int mix,int miy,int max,int may) {
        R.Norm(ref x,ref y,ref x2,ref y2);
        if(x>max||y>=may||x2<mix||y2<miy) return;
        if(x<mix) x=mix;if(x2>max) x2=max;
        if(y<miy) y=miy;if(y2>may) y2=may;
        int h,g;
        if(vertical) {
          for(int a=y;a<=may;a++) {
            g=a*Width;h=a+y2-y+1;if(h>may) h=may;h*=Width;
            g+=mix;h+=mix;
            for(int b=mix;b<=max;b++) Data[g++]=Data[h++];
          }
        }
        if(horizontal) {
          for(int a=x;a<max;a++) {
            g=a;h=a+x2-x+1;if(h>max) h=max;
            g+=miy*Width;h+=miy*Width;
            for(int b=miy;b<=may;b++,g+=Width,h+=Width) Data[g]=Data[h];
          }
        }
      }
      
      
      public bmap Rotate90(bool right) {
        bmap map2=new bmap(Height,Width);
        int d=Height;
        d=right?-d:d;
       unsafe {
        fixed(int* pd2=map2.Data,pd=Data) {        
        for(int y=0;y<Height;y++) {
          int* g2=pd2+(right?(Width-1)*Height+y:(Height-y-1)),h2=pd+y*Width;
          for(int x=0;x<Width;x++) {
            *g2=*h2++;
            g2+=d;
          }
        }
       }}
        return map2;
      }      
      public bmap Rotate90(bool right,int x,int y,int w,int h) {
        if(w<1||h<1||x+w>Width||y+h>Height||x<0||y<0) return null;
        bmap map2=new bmap(h,w);
        int d=right?-h:h;
       unsafe {
        fixed(int *pd2=map2.Data,pd=Data) {
        int *pdx=pd+y*Width+x;
        for(int b=0;b<h;b++) {
          int* g2=pd2+(right?(w-1)*h+b:(h-b-1)),h2=pdx+b*Width;
          for(int a=0;a<w;a++) {
            *g2=*h2++;
            g2+=d;
          }
        }
       }}
        return map2;
      }
      public void CopyRotate90(bmap map2,int x,int y,bool dx,bool dy) {        
        int w=map2.Width,h=map2.Height;
        if(dx) x+=h-w;
        if(dy) y+=w-h;
        CopyRectangle(map2,0,0,w,h,x,y,-1);
      }
      public static double KneeF(int dx,int dy,int x,int y,double a) {
        double x1=x/(dx-a),y1=y/(dy-a);
        return x1*x1+y1*y1-1;
      }
      public static double KneeRadius(int dx,int dy,int x,int y,double min,double max) { // on dx-a/dy-a elipse        
        double qmin=KneeF(dx,dy,x,y,min),qmax=KneeF(dx,dy,x,y,max);
        if(qmin<0==qmax<0) return (min+max)/2;
        for(int i=0;i<10;i++) {
          double qd=qmax-qmin;
          double res=(min+max)/2;
          double q2=KneeF(dx,dy,x,y,res);
          if(q2<0) {min=res;qmin=q2;} else {max=res;qmax=q2;}
        }
        return (min+max)/2;
        //return (min+max)/2;
      }
      public void Knee(int x,int y,int x2,int y2,bool mx,bool my,bool outer) {
        if(!IntersectRect(ref x,ref y,ref x2,ref y2,1,1,Width-2,Height-2)) return;
        int dx=x2-x+1,dy=y2-y+1,p=y*Width+x,px=my?p+dy*Width:p-Width,py=mx?p+dx:p-1;

        for(int y1=0;y1<dy;y1++) {
          double ry=255.0*(my?dy-1-y1:y1)/dy;
          for(int x1=0;x1<dx;x1++) { 
            int ax,ay,ia;
            double rx=255.0*(mx?dx-1-x1:x1)/dx;
            if(rx==0&&ry==0) {
              ax=ay=0;ia=512;
            } else {              
              double r2=rx*rx+ry*ry;
              if(r2>sqr(258)) continue;
              double a=Math.Atan2(ry,rx),r=Math.Sqrt(r2);
              if(outer) {
                //rx=256*(x1-(Math.PI/2-a)*(dx-dy)/2/Math.PI)/dy;
                //r=Math.Sqrt(rx*rx+ry*ry);
                double q=(dx<dy?dx:dy)*(256-r)/256;
                q=KneeRadius(dx,dy,mx?dx-1-x1:x1,my?dy-1-y1:y1,0,dy);
                ax=(int)((dx-q)*256);
                ay=(int)((dy-q)*256);
              } else {
                ax=(int)Math.Floor(dx*r);
                ay=(int)Math.Floor(dy*r);
              }
              if(ax>256*dx||ay>256*dy) continue;
              ia=(int)(a*1024*2/Math.PI);
            }
            int x3=ax&255,y3=ay&255;
            ax/=256;ay/=256;
            int cx=Palette.RGBMix(Data[px+(mx?dx-1-ax:ax)],Data[px+(mx?dx-1-ax-1:ax+1)],x3,256);
            int cy=Palette.RGBMix(Data[py+(my?dy-1-ay:ay)*Width],Data[py+(my?dy-1-ay-1:ay+1)*Width],y3,256);
            //if(mx!=my) ia=1024-ia;
            Data[p+y1*Width+x1]=Palette.RGBMix(cx,cy,ia,1024);
          }        
        }
      }
      public void Corner(int x,int y,int x2,int y2,bool mx,bool my) {
        if(!IntersectRect(ref x,ref y,ref x2,ref y2,1,1,Width-2,Height-2)) return;
        int dx=x2-x+1,dy=y2-y+1,p=y*Width+x,px=my?p+dy*Width:p-Width,py=mx?p+dx:p-1;

        for(int y1=0;y1<dy;y1++) {
          int ry=my?dy-1-y1:y1;
          for(int x1=0;x1<dx;x1++) { 
            int rx=mx?dx-1-x1:x1;
            int ax,ay,ia,a;
            bool h=rx*dy<ry*dx;
            if(h) {
              ay=ry;ax=ry*dx/dy;a=ax;ia=rx;
            } else {
              ax=rx;ay=rx*dy/dx;a=ay;ia=a-ry;
            }
            int cx=Data[px+(mx?dx-1-ax:ax)];
            int cy=Data[py+(my?dy-1-ay:ay)*Width];
            int cm=Palette.RGBMix(cx,cy,1,2);
            if(h) cx=cm;else cy=cm;
            Data[p+y1*Width+x1]=Palette.RGBMix(cy,cx,ia,a);
          }        
        }
      }
      public void Wedge(int x,int y,int x2,int y2,bool hori,bool inv) {
        if(!IntersectRect(ref x,ref y,ref x2,ref y2,1,1,Width-2,Height-2)) return;
        int dx=x2-x+1,dy=y2-y+1,p=y*Width+x,p1=p+(hori?inv?dx:-1:inv?dy*Width:-Width),d1=hori?dy:dx,d2=hori?dx:dy;
        for(int y1=0;y1<dy;y1++) 
          for(int x1=0;x1<dx;x1++) { 
            int r1,r2;
            if(hori) {r1=y1;r2=x1;} else {r1=x1;r2=y1;}
            if(inv) r2=d2-1-r2;
            if(r1<d1/2) {
              if(2*r1*d2<r2*d1) continue;
            } else 
              if(2*(d1-1-r1)*d2<r2*d1) continue;
            Data[p+y1*Width+x1]=Data[p1+r1*(hori?Width:1)];
          }
      }
      public void HLine(int x,int x2,int y,int color) {
        if(x2<x) { int i=x;x=x2;x2=i;}
        for(int h=y*Width+x,he=h+x2-x;h<=he;h++)
          Data[h]=color;
      }
      public void VLine(int x,int y,int y2,int color) {
        if(y2<y) { int i=y;y=y2;y2=i;}
        for(int h=y*Width+x,he=h+(y2-y)*Width;h<=he;h+=Width)
          Data[h]=color;
      }
      public void Strip(int x,int y,int x2,int y2,bool hori,bool inv) {
        if(!IntersectRect(ref x,ref y,ref x2,ref y2,1,1,Width-2,Height-2)) return;
        if(hori) {
          int s=inv?x2+1:x-1;
          for(;y<=y2;y++)
            HLine(x,x2,y,Data[y*Width+s]);
        } else {
          int s=(inv?y2+1:y-1)*Width;
          for(;x<=x2;x++)
            VLine(x,y,y2,Data[s+x]);
        }
      }
      public void Cone(int x,int y,int x2,int y2,bool inv,int bgcolor) {
        int cx=x+x2;
        if(!IntersectRect(ref x,ref y,ref x2,ref y2,1,1,Width-2,Height-2)) return;
        cx-=2*x;
        int h=y*Width+x,g,dx=x2-x,dy=y2-y; 
        int[] res=new int[dx+1];
        for(int y1=0,yi=inv?2*dy+1:1;y1<=dy;y1++,yi+=inv?-2:2) {          
          g=(y+y1)*Width+x;
          if(yi==0) {
            Data[g+cx]=Palette.RGBAvg(Data,h,1,cx*256,cx*256);  
          } else
            for(int x1=0,xi=-cx;x1<=dx;x1++,xi+=2) { 
              int from=128*cx+128*(xi*2*(dy+1))/yi;
              int to=128*cx+128*((xi+2)*2*(dy+1))/yi;
              if(from<256*dx&&to>0)
                res[x1]=Palette.RGBAvg(Data,g,1,from,to);
              else
                res[x1]=bgcolor;
            }
          Array.Copy(res,0,Data,g,dx+1);
        }
      }


      public static Bitmap Rotate90(Bitmap bm,bool right) {
        if(bm==null) return null;
        bmap x=FromBitmap(null,bm,-2);
        x=x.Rotate90(right);
        Bitmap ret=new Bitmap(bm.Height,bm.Width,bm.PixelFormat);
        ToBitmap(x,1,1,ret,0,0,ret.Width-1,ret.Height-1,IsAlpha(bm));
        return ret;
      }
      public static void Mirror(Bitmap bm,bool vertical,bool horizontal) {
        if(bm==null) return;
        bmap x=FromBitmap(null,bm,-2);
        x.Mirror(vertical,horizontal);
        ToBitmap(x,1,1,bm,0,0,bm.Width-1,bm.Height-1,IsAlpha(bm));
      }
      public void Bold(bool rgb,bool max,bool vert,int x,int y,int x2,int y2) {
        R.Norm(ref x,ref y,ref x2,ref y2);
				if(!IntersectRect(ref x,ref y,ref x2,ref y2,0,0,Width-1,Height-1)) return;
        if(vert) {
          for(;y2>y;y2--) 
            for(int p2=Width*y2+x,p=p2-Width,pe=p+x2-x;p<pe;p++,p2++) {
              int c=Data[p],c2=Data[p2];
              c=max?Palette.RGBMax(c,c2,rgb):Palette.RGBMin(c2,c,rgb);
              if(c!=c2) {
                Data[p2]=c;
                c=c2;
              }
            }
        } else {
          for(;y<=y2;y++)
            for(int p=Width*y+x,c=Data[p],pe=p+++x2-x;p<pe;p++) {
              int c2=Data[p];
              c=max?Palette.RGBMax(c,c2,rgb):Palette.RGBMin(c2,c,rgb);
              if(c!=c2) {
                Data[p]=c;
                c=c2;
              }
            }              
        }
      }
      public void vmove(int dst,int src,int count2,int step2,int count,int step) {
        if(count<0) {
          src+=(count+1)*Width;dst+=(count+1)*Width;
          count*=-1;
        }
        if(dst>src) {
          src+=(count-1)*step;dst+=(count-1)*step;
          step*=-1;
        }
        unsafe{ fixed(int* pd=Data) {
         for(;count2-->0;dst+=step2,src+=step2) 
          for(int c=count,d=dst,s=src;c-->0;d+=step,s+=step) pd[d]=pd[s];
        }}
      }
      public void vline(int dst,int color,int count,int step) {
        unsafe{ fixed(int* pd=Data) {
          while(count-->0) {
            pd[dst]=color;
            dst+=step;
          }
        }}
      }
      public int Comp(bool hori,bool vert,int lim,int x,int y,int x2,int y2) {
				if(!IntersectRect(ref x,ref y,ref x2,ref y2,0,0,Width-1,Height-1)) return -1;
        int xx,yy,g,k,h;//        unsafe{ fixed(int* ff=fifo,pd=Data) {
        int n,e,d,width=x2-x+1,height=y2-y+1,nw=width,nh=height;
        if(lim<2) lim=2;
        if(hori) {
          n=1;e=0;g=h=Width*y+x;
          for(xx=1;xx<=width;xx++) {
            yy=0;
            if(xx<width) for(k=y*Width+x+xx;yy<height&&(Data[k-1]&White)==(Data[k]&White);yy++,k+=Width);
            d=0;
            if(yy==height) {
              e++;
            } else {
              if(e>lim||xx==width) {d=n+(e<lim?e:lim);e=0;n=1;}
              else {n+=e+1;e=0;}
            }
            if(d>0) {
              if(g<h) vmove(g,h,height,Width,d,1);
              g+=d;h=y*Width+x+xx;
            }
          }
          if(0<(d=y*Width+x+width-g)) {
            for(yy=0,nw-=d;yy<height;yy++,g+=Width)
              vline(g,Data[g-1],d,1);
          }
        }
        if(vert) {
          n=1;e=0;g=h=y*Width+x;
          for(yy=1;yy<=height;yy++) {
            xx=0;
            if(yy<height) for(k=y*Width+x+Width*yy;xx<width&&(Data[k-Width]&White)==(Data[k]&White);xx++,k++);
            d=0;
            if(xx==width) {
              e++;
            } else {
              if(e>lim||yy==height) {d=n+(e<lim?e:lim);e=0;n=1;}
              else {n+=e+1;e=0;}
            }
            if(d>0) {
              if(g<h) vmove(g,h,width,1,d,Width);
              g+=d*Width;h=y*Width+x+Width*yy;
            }
          }
          if(0<(d=y*Width+x+Width*height-g)) {
            for(xx=0,d/=Width,nh-=d;xx<width;xx++,g++)
              vline(g,Data[g-Width],d,Width);
          }
        }
        return (nw<<16)|nh;
      }

      public delegate int Filter1Delegate(object param,int c);
      public int Filter1(Filter1Delegate filter,object param,int x,int y,int x2,int y2,int alpha) {
        R.Norm(ref x,ref y,ref x2,ref y2);
				if(!IntersectRect(ref x,ref y,ref x2,ref y2,0,0,Width-1,Height-1)) return -1;
        if(x>x2||y>y2) return -1;
        int g=y*Width+x,n=0,ax=256-alpha;
        for(;y<=y2;y++) {
          for(int ge=g+x2-x;g<=ge;g++) {
            int c2=Data[g]&White,c=filter(param,c2);
            if(c==-1) continue;
            c&=White;
            if(c!=c2) { 
              if(alpha!=0) {
                int a0=ax*(c&255)+alpha*(c2&255),a1=ax*((c>>8)&255)+alpha*((c2>>8)&255),a2=ax*((c>>8))+alpha*((c2>>8));
                c=(a0>>8)|(a1&0xff00)|(a2&0xff0000);                
              }
              Data[g]=c;
              n++;
            }
          }
          g+=Width-(x2-x+1);
        }
        return n;
      }
      public static int Filter1Replace(object param,int c) {
        int[] i2=param as int[];
        return c==i2[0]?i2[1]:-1;
      }
      public void Filter(FilterOp f,int param,bool bw,int x,int y,int x2,int y2,bmap undo) {
        R.Norm(ref x,ref y,ref x2,ref y2);
				if(!IntersectRect(ref x,ref y,ref x2,ref y2,0,0,Width-1,Height-1)) return;
        if(x>x2||y>y2) return;
        int g=y*Width+x;
        bool bwi=bw&&(f==FilterOp.Invert||f==FilterOp.InvertIntensity);
        byte[] ba=null;
        if(f==FilterOp.Contrast) {
          ba=new byte[6];
          ba[0]=ba[1]=ba[2]=255;
          for(int yy=y,gg=g;yy<=y2;yy++) {
            for(int ge=gg+x2-x;gg<=ge;gg++) {
              int c=Data[gg];
              byte c0=(byte)(c&255),c1=(byte)((c>>8)&255),c2=(byte)((c>>16)&255);
              if(c0<ba[0]) ba[0]=c0;else if(c0>ba[3]) ba[3]=c0;
              if(c1<ba[1]) ba[1]=c1;else if(c1>ba[4]) ba[4]=c1;
              if(c2<ba[2]) ba[2]=c2;else if(c2>ba[5]) ba[5]=c2;
            }
            gg+=Width-(x2-x+1);
          }
          if(ba[3]<=ba[0]) ba[3]=ba[0];
          if(ba[4]<=ba[1]) ba[4]=ba[1];
          if(ba[5]<=ba[2]) ba[5]=ba[2];
          if(!bw) {
            if(ba[1]<ba[0]) ba[0]=ba[1];
            if(ba[2]<ba[0]) ba[0]=ba[2];
            ba[1]=ba[2]=ba[0];
            if(ba[4]>ba[3]) ba[3]=ba[4];
            if(ba[5]>ba[3]) ba[3]=ba[5];
            ba[4]=ba[5]=ba[3];
          }
        }
        for(;y<=y2;y++) {
          for(int ge=g+x2-x;g<=ge;g++) {
            int c=Data[g]&White;
            if(f==FilterOp.Contrast) {
              byte c0=(byte)(c&255),c1=(byte)((c>>8)&255),c2=(byte)((c>>16)&255);
              if(ba[0]!=ba[3]) c0=(byte)((c0-ba[0])*255/(ba[3]-ba[0]));
              if(ba[1]!=ba[4]) c1=(byte)((c1-ba[1])*255/(ba[4]-ba[1]));
              if(ba[2]!=ba[5]) c2=(byte)((c2-ba[2])*255/(ba[5]-ba[2]));
              Data[g]=c0|(c1<<8)|(c2<<16);
              continue;
            }
            if(f==FilterOp.Border||f==FilterOp.Border2) {
              byte c0=(byte)(c&255),c1=(byte)((c>>8)&255),c2=(byte)((c>>16)&255);
              int csum=c0+c1+c2;
              bool wh=false,go=false;
              for(int i=0,n=bw?8:4;i<n;i++) {
                int d=undo.Data[g+(i<4?i<2?i==1?1:-1:i==2?-Width:Width:i<6?i==5?-Width+1:-Width-1:i==6?Width-1:Width+1)]&White;
                byte d0=(byte)(d&255),d1=(byte)((d>>8)&255),d2=(byte)((d>>16)&255);
                int dsum=d0+d1+d2;
                if(csum>dsum||(csum==dsum&&c>d)) continue;
                //if(d0>c0+param||d1>c1+param||d2>c2+param) {
                if(Palette.RGBDiff(2,param,c,d)) {
                  go=true;
                  break;
                }
              }
              if(go&&f==FilterOp.Border) Data[g]=0;
              else if(!go&&f!=FilterOp.Border) Data[g]=White;
              continue;
            }
            if(c==0||c==White) {
              if(bwi) Data[g]=c==0?White:0; 
            } else if(f==FilterOp.InvertIntensity) Data[g]=Palette.InvertIntensity(c,0);
            else if(f==FilterOp.Saturate) Data[g]=Palette.Saturate(c,param);
            else if(f==FilterOp.Grayscale) Data[g]=Palette.Grayscale(c,param);
            else if(f==FilterOp.Levels) Data[g]=Palette.Levels(c,param);
            else if(f==FilterOp.Strips) Data[g]=Palette.Strips(c,param,0,White);
            else if(f==FilterOp.ToWhite) Data[g]=Palette.ToWhite(c,param);
            else if(f==FilterOp.ToBlack) Data[g]=Palette.ToBlack(c,param);
            else if(f==FilterOp.Channel) {
              bool rgb=0!=(param&16);
              int p=param&15;
              int i;
              if(p<3) i=(p==1?c>>8:p==0?c>>16:c)&255;
              else if(p==6) i=isqrt(Palette.RGBSqr(c,0))*255/441;
              else if(p>=7) {
                int c0=c&255,c1=(c>>8)&255,c2=(c>>16)&255;
                if(p==7) {i=c0<c1?c0:c1;if(c2<i) i=c2;}
                else if(p==9) {i=c0>c1?c0:c1;if(c2>i) i=c2;}
                else i=(c0+c1+c2)/3;
              }
              else {
                int c0=((c>>(p==3?16:0))&255),c1=(c>>(p==5?16:8))&255;
                i=c0<c1?c0:c1;
              }
              if(rgb)
                Data[g]=Palette.ColorIntensity765(c,i*3,true);
              else
                Data[g]=i*0x10101;
            } else if(f==FilterOp.Substract) Data[g]=Palette.RGBSub(c,param,(param>>24)&7,0!=((param>>28)&1));
            else if(f==FilterOp.Hue) Data[g]=Palette.RGBHue(c,0!=(param&4096),param&2047);
            else if(f==FilterOp.Perm) {
              int p,r=0;
              p=param&255;r=((c>>((p&3)*8))^(0==(p&16)?0:255))&255;
              p=(param>>8)&255;r|=(((c>>((p&3)*8))^(0==(p&16)?0:255))&255)<<8;
              p=(param>>16)&255;r|=(((c>>((p&3)*8))^(0==(p&16)?0:255))&255)<<16;
              Data[g]=r;
            } else Data[g]^=White;                        
          }
          g+=Width-(x2-x+1);
        }
      }
      public static int Filter256(object map,int c) {
        byte[] m=map as byte[];
        return Palette.RGBMap(c,m);
      }
      public static int Filter765c(object map,int c) {
        int[] m=map as int[];
        return Palette.RGBMapic(c,m);
      }
      public static int Filter765i(object map,int c) {
        int[] m=map as int[];
        return Palette.RGBMapii(c,m,true);
      }
      public static int FilterSatur(object desat,int c) {
        char ch=(char)desat;
        return Palette.Saturate(c,ch=='d',ch=='o'); 
      }
      public static int FilterReplace(object sr,int c) {
        int[] ia=sr as int[];
        return Palette.RGBReplace(c,ia[0],ia[1],ia[2]);
      }
      public delegate int Filter33Delegate(bmap map,int offset,object param);
      public void Filter33(Filter33Delegate fx,object param,bool bw,int x,int y,int x2,int y2,bmap undo) {
        R.Norm(ref x,ref y,ref x2,ref y2);
				if(!IntersectRect(ref x,ref y,ref x2,ref y2,1,1,Width-2,Height-2)) return;
        if(x>x2||y>y2) return;
        undo.Border();
        for(;y<=y2;y++) 
          for(int p=y*Width+x,pe=p+x2-x;p<=pe;p++) {
            if(!bw&&((undo.Data[p]&White)==White||(undo.Data[p]&White)==0)) continue;
            Data[p]=fx(undo,p,param);
          }
      }
      public static int Filter33Emboss(bmap map,int p,object param) {
        int r=0,b=0,g=0,div=param==null?4:(int)param;
        if(div==0) div=1;
        Palette.RGBAdd(map.Data[p-1],10,ref b,ref r,ref g);
        Palette.RGBAdd(map.Data[p+map.Width],10,ref b,ref r,ref g);
        Palette.RGBAdd(map.Data[p+map.Width-1],4,ref b,ref r,ref g);
        Palette.RGBAdd(map.Data[p],-24,ref b,ref r,ref g);
        r=128-r/div;if(r<0) r=0;else if(r>255) r=255;
        g=128-g/div;if(g<0) g=0;else if(g>255) g=255;
        b=128-b/div;if(b<0) b=0;else if(b>255) b=255;
        return b|(r<<8)|(g<<16);
      }
      public static int Filter33Blur(bmap map,int p,object param) {
        int r=0,b=0,g=0;
        Palette.RGBAdd(map.Data[p-1],ref b,ref r,ref g);
        Palette.RGBAdd(map.Data[p+1],ref b,ref r,ref g);
        Palette.RGBAdd(map.Data[p-map.Width],ref b,ref r,ref g);
        Palette.RGBAdd(map.Data[p+map.Width],ref b,ref r,ref g);
        Palette.RGBAdd(map.Data[p],4,ref b,ref r,ref g);
        return Palette.RGBAvg(8,b,r,g);
      }
      public static int Filter33Sharp(bmap map,int p,object param) {
        int r=0,b=0,g=0;
        Palette.RGBAdd(map.Data[p-1],-1,ref b,ref r,ref g);
        Palette.RGBAdd(map.Data[p+1],-1,ref b,ref r,ref g);
        Palette.RGBAdd(map.Data[p-map.Width],-1,ref b,ref r,ref g);
        Palette.RGBAdd(map.Data[p+map.Width],-1,ref b,ref r,ref g);
        Palette.RGBAdd(map.Data[p-map.Width-1],-2,ref b,ref r,ref g);
        Palette.RGBAdd(map.Data[p-map.Width+1],-2,ref b,ref r,ref g);
        Palette.RGBAdd(map.Data[p+map.Width-1],-2,ref b,ref r,ref g);
        Palette.RGBAdd(map.Data[p+map.Width+1],-2,ref b,ref r,ref g);
        Palette.RGBAdd(map.Data[p],24,ref b,ref r,ref g);
        return Palette.RGBAvg2(12,b,r,g);
      }
      public static int Filter33Min(bmap map,int p,object param) {
        int r=0,b=0,g=0;
        Palette.RGBMin(map.Data[p-1],ref b,ref r,ref g);
        Palette.RGBMin(map.Data[p+1],ref b,ref r,ref g);
        Palette.RGBMin(map.Data[p-map.Width],ref b,ref r,ref g);
        Palette.RGBMin(map.Data[p+map.Width],ref b,ref r,ref g);
        Palette.RGBMin(map.Data[p],ref b,ref r,ref g);
        return Palette.RGBAvg(1,b,r,g);
      }
      public static int Filter33Edge(bmap map,int p,object param) {
        int i0=255,i1=255,i2=255,a0=0,a1=0,a2=0;
        Palette.RGBMinMax(map.Data[p],ref i0,ref i1,ref i2,ref a0,ref a1,ref a2);
        Palette.RGBMinMax(map.Data[p+1],ref i0,ref i1,ref i2,ref a0,ref a1,ref a2);
        Palette.RGBMinMax(map.Data[p+map.Width],ref i0,ref i1,ref i2,ref a0,ref a1,ref a2);
        int mul=(int)param;
        a0=255-mul*(a0-i0);
        a1=255-mul*(a1-i1);
        a2=255-mul*(a2-i2);
        return Palette.RGBAvg2(1,a0,a1,a2);
      }
      public static int Filter33Edge1(bmap map,int p,object param) {
        int a0=0,a1=0,a2=0;
        int c,c2,c1,c0,e,d;
        c=map.Data[p];c0=c&255;c1=(c>>8)&255;c2=(c>>16)&255;
        e=map.Data[p-1];d=(e&255)-c0;if(d>a0) a0=d;d=((e>>8)&255)-c1;if(d>a1) a1=d;d=((e>>16)&255)-c2;if(d>a2) a2=d;
        e=map.Data[p+1];d=(e&255)-c0;if(d>a0) a0=d;d=((e>>8)&255)-c1;if(d>a1) a1=d;d=((e>>16)&255)-c2;if(d>a2) a2=d;
        e=map.Data[p-map.Width];d=(e&255)-c0;if(d>a0) a0=d;d=((e>>8)&255)-c1;if(d>a1) a1=d;d=((e>>16)&255)-c2;if(d>a2) a2=d;
        e=map.Data[p+map.Width];d=(e&255)-c0;if(d>a0) a0=d;d=((e>>8)&255)-c1;if(d>a1) a1=d;d=((e>>16)&255)-c2;if(d>a2) a2=d;
        int mul=(int)param;
        a0=255-mul*a0;
        a1=255-mul*a1;
        a2=255-mul*a2;
        return Palette.RGBAvg2(1,a0,a1,a2);
      }
      public static int Filter33Neq(bmap map,int p,object param) {
        bool x8=param!=null,eq;
        int c=map.Data[p];
        eq=c==map.Data[p-1]&&c==map.Data[p+1]&&c==map.Data[p-map.Width]&&c==map.Data[p+map.Width];
        if(eq&&x8) 
          eq=c==map.Data[p-map.Width-1]&&c==map.Data[p-map.Width+1]&&c==map.Data[p+map.Width-1]&&c==map.Data[p+map.Width+1];
        return eq?White:c;
      }
      public static int Filter33HDR(bmap map,int p,object param) {
        bool x8=param!=null;
        int c,min=255,max=0,avg=0;
        Palette.RGBMinMax(c=map.Data[p],ref min,ref max,ref avg);
        Palette.RGBMinMax(map.Data[p-1],ref min,ref max,ref avg);
        Palette.RGBMinMax(map.Data[p+1],ref min,ref max,ref avg);
        Palette.RGBMinMax(map.Data[p-map.Width],ref min,ref max,ref avg);
        Palette.RGBMinMax(map.Data[p+map.Width],ref min,ref max,ref avg);
        if(x8) { //&&(min>0||max<255)
          Palette.RGBMinMax(map.Data[p-map.Width-1],ref min,ref max,ref avg);
          Palette.RGBMinMax(map.Data[p-map.Width+1],ref min,ref max,ref avg);
          Palette.RGBMinMax(map.Data[p+map.Width-1],ref min,ref max,ref avg);
          Palette.RGBMinMax(map.Data[p+map.Width+1],ref min,ref max,ref avg);
        }
        //if(min==0&&max==255||(min==max)) return c;
        if(min==max) return c;
        avg/=x8?27:15;
        int b=c&255,g=(c>>8)&255,r=(c>>16)&255,b2,g2,r2;
        //int b2=(b-min)*255/(max-min),g2=(g-min)*255/(max-min),r2=(r-min)*255/(max-min);
        b2=b==min?0:b==max?255:b>=avg?128+(b-avg)*127/(max-avg):(b-min)*127/(avg-min);
        g2=g==min?0:g==max?255:g>=avg?128+(g-avg)*127/(max-avg):(g-min)*127/(avg-min);
        r2=r==min?0:r==max?255:r>=avg?128+(r-avg)*127/(max-avg):(r-min)*127/(avg-min);
        b=(3*b+b2)/4;g=(3*g+g2)/4;r=(3*r+r2)/4;
        return b|(g<<8)|(r<<16);
      }
      public static int dia(int x,int half,int max) {
        bool neg=x<0;
        if(neg) x=-x;
        if(x<max) x=2*x>max?max-(max-x)*(max-half)*2/max:x*half*2/max;
        return neg?-x:x;
      }
      public static byte sharp1(byte c,byte a,int mode,int mul) {
        int x;
        if(mode==2) x=dia(mul*(c-a),96,128)+128;
        else if(mode==3) x=mul*(a-c);
        else if(mode==4) x=255-dia(mul*(a-c),192,256);
        else if(mode==5) x=255-dia(mul*(c-a),192,256);
        else x=2*c-a;
        return (byte)(x<0?0:x>255?255:x);
      }
      public static int sharp3(int c,int a,int mode,int mul,bool satur) {
        int sc=Palette.RGBSum(c),sa=Palette.RGBSum(a),x;
        if(mode==2) x=dia(mul*(sc-sa),256,384)+383;        
        else if(mode==3) x=dia(mul*(sc-sa),512,765);
        else if(mode==4) x=765-dia(mul*(sa-sc),512,765);
        else if(mode==5) x=765-dia(mul*(sc-sa),512,765);
        else x=2*sc-sa;
        c=Palette.ColorIntensity765(c,x,satur);
        return c;
      }
      public void Blur(int size,bool hori,bool vert,int sharp,int mul,int x,int y,int x2,int y2) {
        if(size<1||!(hori||vert)) return;
        R.Norm(ref x,ref y,ref x2,ref y2);
				if(!IntersectRect(ref x,ref y,ref x2,ref y2,1,1,Width-2,Height-2)) return;
        if(x>x2||y>y2) return;
        bmap copy=new bmap(null,x,y,x2,y2,0);
        int pixel=Width*y+x,h,g,c,width=copy.Width,height=copy.Height;
        int x1,y1,s0,s1,s2,n,s4;    
        byte c0,c1,c2;
        if(hori) {
          s4=size;
          for(y1=0;y1<height;y1++) {
             for(x1=s0=s1=s2=n=0,h=pixel+y1*Width;x1<width&&x1<size;x1++,h++) 
	             { Palette.RGBAdd(Data[h],ref s0,ref s1,ref s2);n++;}
             for(x1=0,h=pixel+y1*Width,g=y1*width;x1<width;x1++,h++,g++) {
	             if(x1+size<width) {Palette.RGBAdd(Data[h+s4],ref s0,ref s1,ref s2);n++;}
	             c0=(byte)((s0+size)/n);c1=(byte)((s1+size)/n);c2=(byte)((s2+size)/n);
               if(sharp>0&&!vert) {
                 c=Data[h]; 
                 if(sharp>=8) {
                   c=sharp3(c,c0|(c1<<8)|(c2<<16),sharp-8,mul,false);
                   c0=(byte)(c&255);c1=(byte)((c>>8)&255);c2=(byte)((c>>16)&255);
                 } else {
                   c0=sharp1((byte)(c&255),c0,sharp,mul);c1=sharp1((byte)((c>>8)&255),c1,sharp,mul);c2=sharp1((byte)((c>>16)&255),c2,sharp,mul);
                 }
               }
               copy.Data[g]=c0|(c1<<8)|(c2<<16);
	             if(x1>=size) {Palette.RGBAdd(Data[h-s4],-1,ref s0,ref s1,ref s2);n--;}
             }
          }
        } else
          copy.CopyRectangle(this,x,y,x2,y2,0,0,-1);          
        if(vert) {
          s4=width*size;
          for(x1=0;x1<width;x1++) {
             for(y1=s0=s1=s2=n=0,h=x1;y1<height&&y1<size;y1++,h+=width) 
	             { Palette.RGBAdd(copy.Data[h],ref s0,ref s1,ref s2);n++;}
             for(y1=0,h=x1,g=y*Width+x+x1;y1<height;y1++,h+=width,g+=Width) {
	             if(y1+size<height) { Palette.RGBAdd(copy.Data[h+s4],ref s0,ref s1,ref s2);n++;}
	             c0=(byte)((s0+size)/n);c1=(byte)((s1+size)/n);c2=(byte)((s2+size)/n);
               if(sharp>0) {
                 c=Data[g];                  
                 if(sharp>=8) {
                   c=sharp3(c,c0|(c1<<8)|(c2<<16),sharp-8,mul,false);
                   c0=(byte)(c&255);c1=(byte)((c>>8)&255);c2=(byte)((c>>16)&255);
                 } else {
                   c0=sharp1((byte)(c&255),c0,sharp,mul);c1=sharp1((byte)((c>>8)&255),c1,sharp,mul);c2=sharp1((byte)((c>>16)&255),c2,sharp,mul);
                 }                   
               }
               Data[g]=c0|(c1<<8)|(c2<<16);
	             if(y1>=size) {Palette.RGBAdd(copy.Data[h-s4],-1,ref s0,ref s1,ref s2);n--;}
             }
          }
        } else
          CopyRectangle(copy,0,0,width-1,height-1,x,y,-1);
      }
      public void Blur2(int size,int shape,int sharp,int mul,int x1,int y1,int x2,int y2) {
        bmap copy=new bmap(this,x1,y1,x2,y2,0);
        R.Norm(ref x1,ref y1,ref x2,ref y2);
				if(!IntersectRect(ref x1,ref y1,ref x2,ref y2,1,1,Width-2,Height-2)) return;
        if(x1>x2||y1>y2) return;
        int pixel=Width*y1+x1,h,g,c,width=copy.Width,height=copy.Height;
        byte[] sa=new byte[2*size+1];
        byte c0,c1,c2;
        int x,y,z,k,s0,s1,s2,z0,z1,n,t,sz;
        if(size<1) return;
        t=sa[size]=(byte)size;
        for(z=1;z<=size;z++)
          t+=1+2*(sa[size+z]=sa[size-z]=(byte)(shape==1?Math.Min(size,3*size/2-z):shape==2?size:size-z));
        for(y=0;y<height;y++) {
           z0=y<size?-y:-size;
           z1=y+size>=height?height-1-y:size;
           for(s0=s1=s2=n=0,k=pixel+y*Width,z=z0;z<=z1;z++)
             for(h=k+z*Width,sz=sa[size+z],x=0;x<width&&x<sz;x++,h++) 
               { Palette.RGBAdd(Data[h],ref s0,ref s1,ref s2);n++;}               
           for(x=0,g=y*copy.Width;x<width;x++,g++,k++) {
             for(z=z0,h=k+z*Width;z<=z1;z++,h+=Width)
               if(x+(sz=sa[size+z])<width) { Palette.RGBAdd(Data[h+sz],ref s0,ref s1,ref s2);n++;} 
	            c0=(byte)((s0+size)/n);c1=(byte)((s1+size)/n);c2=(byte)((s2+size)/n);
              if(sharp>0) {
                c=Data[k];                  
                if(sharp>=8) {
                  c=sharp3(c,c0|(c1<<8)|(c2<<16),sharp-8,mul,false);
                  c0=(byte)(c&255);c1=(byte)((c>>8)&255);c2=(byte)((c>>16)&255);
                } else {
                  c0=sharp1((byte)(c&255),c0,sharp,mul);c1=sharp1((byte)((c>>8)&255),c1,sharp,mul);c2=sharp1((byte)((c>>16)&255),c2,sharp,mul);
                }                   
              }
             copy.Data[g]=c0|(c1<<8)|(c2<<16);
             for(z=z0,h=k+z*Width;z<=z1;z++,h+=Width)
               if(x>=(sz=sa[size+z]))
                 { Palette.RGBAdd(Data[h-sz],-1,ref s0,ref s1,ref s2);n--;}                 
           }
        }
        CopyRectangle(copy,0,0,width-1,height-1,x1,y1,-1);
      }

      public void Shadow(int count,int color,int x,int y,int x2,int y2) {
        R.Norm(ref x,ref y,ref x2,ref y2);
				if(!IntersectRect(ref x,ref y,ref x2,ref y2,1,1,Width-2,Height-2)) return;
        Border();
        ClearByte(x,y,x2,y2,0);
        RectByte(x-1,y-1,x2+1,y2+1,1);
        bool all=color<0;
        for(int y1=y2;y1>=y;y1--) {
          for(int p=y1*Width+x,pe=p+(x2-x+1);p<pe;p++) {
            int q=p,c=Data[q];
            if(all) color=c;
            if(c!=color||(White&Data[q-Width-1])==color) continue;
            for(int i=1;i<count;i++) {
              q+=Width+1;
              if(Data[q]!=color) break;
              Data[q]=Palette.RGBMix(0x808080,color,i,count);
            }
          }
        }
        RectByte(x-1,y-1,x2+1,y2+1,0);
      }
      public void Color256(int mode,int x,int y,int x2,int y2) {
        R.Norm(ref x,ref y,ref x2,ref y2);
				if(!IntersectRect(ref x,ref y,ref x2,ref y2,1,1,Width-2,Height-2)) return;
        if(x>x2||y>y2) return;
        int[] sum=new int[(mode==8?8:mode==6?64:mode>=4?4096:mode==3?512:256)*4];
        for(int y3=y;y3<=y2;y3++) 
          for(int p=y3*Width+x,pe=p+x2-x;p<=pe;p++) {
            int c=Data[p],b=c&255,g=(c>>8)&255,r=(c>>16)&255,i;
            if(mode==6) 
              i=b/64*16+g/64*4+r/64;
            else if(mode==8) 
              i=b/128*4+g/128*2+r/128;
            else if(mode==4) 
              i=b/16*256+g/16*16+r/16;
            else if(mode==3) 
              i=b/32*64+g/32*8+r/32;            
            else if(mode==2) {
              int s=r+g+b,d=abs(r-g)+abs(g-b)+abs(b-r),rgb=((r&128)!=0?4:0)|((g&128)!=0?2:0)|((b&128)!=0?1:0);
              i=(s+2)/96*32+(d+1)/128*8+rgb;
            } else if(mode==1) {
              i=(r+38)/42*35+(g+38)/42*5+(b+4)/52;
            } else {
              i=r/32*32+g/32*4+b/64;
            }
            i*=4;
            sum[i]++;sum[i+1]+=b;sum[i+2]+=g;sum[i+3]+=r;
          }
        for(int i=0;i<sum.Length;i+=4) 
          if(sum[i]!=0) sum[i]=Palette.RGBAvg(sum[i],sum[i+1],sum[i+2],sum[i+3]);
        for(int y3=y;y3<=y2;y3++) 
          for(int p=y3*Width+x,pe=p+x2-x;p<=pe;p++) {
            int c=Data[p],b=c&255,g=(c>>8)&255,r=(c>>16)&255,i;
            if(mode==6) 
              i=b/64*16+g/64*4+r/64;
            else if(mode==8) 
              i=b/128*4+g/128*2+r/128;
            else if(mode==4) 
              i=b/16*256+g/16*16+r/16;
            else if(mode==3)  
              i=b/32*64+g/32*8+r/32;            
            else if(mode==2) {
              int s=r+g+b,d=abs(r-g)+abs(g-b)+abs(b-r),rgb=((r&128)!=0?4:0)|((g&128)!=0?2:0)|((b&128)!=0?1:0);
              i=(s+2)/96*32+(d+1)/128*8+rgb;
            } else if(mode==1) {
              i=(r+38)/42*35+(g+38)/42*5+(b+4)/52;
            } else {
              i=r/32*32+g/32*4+b/64;
            }
            Data[p]=sum[4*i];
          }         
      }
      public void Color765(int satur,int x,int y,int x2,int y2) {
        R.Norm(ref x,ref y,ref x2,ref y2);
				if(!IntersectRect(ref x,ref y,ref x2,ref y2,1,1,Width-2,Height-2)) return;
        if(x>x2||y>y2) return;
        int[] sum=new int[766];
        int i,s=Palette.RGBSum(Data[y*Width+x]),si,c,n=0,min=s,max=s;
        for(int y3=y;y3<=y2;y3++) 
          for(int p=y3*Width+x,pe=p+x2-x;p<=pe;p++) {
            i=Palette.RGBSum(Data[p]);
            if(i<min) min=i;else if(i>max) max=i;
            sum[i]++;
            n++;
          }
        if(min==max) return;
        n-=sum[min]+sum[max];
        bool limit=true;
        if(limit) {
          int l=n/254;
          for(i=min+1;i<max;i++) 
            if((si=sum[i])>l) { n-=si-l;sum[i]=l;}  
        }
        for(s=0,i=min+1;i<max;i++)
          if(sum[i]!=0) {
            si=sum[i];
            s+=si;
            sum[i]=1+(int)(s*764L/n);
          }
        for(int y3=y;y3<=y2;y3++) 
          for(int p=y3*Width+x,pe=p+x2-x;p<=pe;p++) {
            c=Data[p];
            int s0=c&255,s1=(c>>8)&255,s2=(c>>16)&255;
            i=s0+s1+s2;
            if(i<=min) c=0;
            else if(i>=max) c=White;
            else {
              s=sum[i];
              c=Palette.ColorIntensity765(s0,s1,s2,s,satur);
            }
            Data[p]=c;
          }        
      }
      public void Color765rgb(int x,int y,int x2,int y2) {
        R.Norm(ref x,ref y,ref x2,ref y2);
				if(!IntersectRect(ref x,ref y,ref x2,ref y2,1,1,Width-2,Height-2)) return;
        if(x>x2||y>y2) return;
        int[] s0=new int[256],s1=new int[256],s2=new int[256];
        int c=Data[y*Width+x],ph0,ph1,ph2,s0i=c&255,s1i=(c>>8)&255,s2i=(c>>16)&255,s0a=s0i,s1a=s1i,s2a=s2i,n0,n1,n2,s,i,si;
        for(int y3=y;y3<=y2;y3++) 
          for(int p=y3*Width+x,pe=p+x2-x;p<=pe;p++) {
            c=Data[p];
            s0[ph0=c&255]++;s1[ph1=(c>>8)&255]++;s2[ph2=(c>>16)&255]++;
            if(ph0<s0i) s0i=ph0;else if(ph0>s0a) s0a=ph0;
            if(ph1<s1i) s1i=ph1;else if(ph1>s1a) s1a=ph1;
            if(ph2<s2i) s2i=ph2;else if(ph2>s2a) s2a=ph2;
          }
        n0=n1=n2=(x2-x+1)*(y2-y+1);
        if(s0i==s0a&&s1i==s1a&&s2i==s2a) return;
        n0-=s0[s0i]+s0[s0a];
        n1-=s1[s1i]+s1[s1a];
        n2-=s2[s2i]+s2[s2a];
        if(n0<1&&n1<1&&n2<1) return;
        bool limit=true;
        if(limit) {
          int l0=n0/254,l1=n1/254,l2=n2/254;
          for(i=0;i<255;i++) {
            if(i>s0i&&i<s0a&&(si=s0[i])>l0) {n0-=si-l0;s0[i]=l0;}
            if(i>s1i&&i<s1a&&(si=s1[i])>l1) {n1-=si-l1;s1[i]=l1;}
            if(i>s2i&&i<s2a&&(si=s2[i])>l2) {n2-=si-l2;s2[i]=l2;}
          }  
        }
        if(s0i<s0a) {s0[s0i]=0;s0[s0a]=255;}
        if(s1i<s1a) {s1[s1i]=0;s1[s1a]=255;}
        if(s2i<s2a) {s2[s2i]=0;s2[s2a]=255;}
        n0++;n1++;n2++;
        for(s=0,i=s0i+1;i<s0a;i++)
          if((si=s0[i])>0) { s+=si; s0[i]=s*255/n0; }
        for(s=0,i=s1i+1;i<s1a;i++)
          if((si=s1[i])>0) { s+=si; s1[i]=s*255/n1; }
        for(s=0,i=s2i+1;i<s2a;i++)
          if((si=s2[i])>0) { s+=si; s2[i]=s*255/n2; }
        for(int y3=y;y3<=y2;y3++) 
          for(int p=y3*Width+x,pe=p+x2-x;p<=pe;p++) {
            c=Data[p];
            ph0=c&255;ph1=(c>>8)&255;ph2=(c>>16)&255;
            if(s0i<s0a) ph0=s0[ph0];
            if(s1i<s1a) ph1=s1[ph1];
            if(s2i<s2a) ph2=s2[ph2];
            Data[p]=ph0|(ph1<<8)|(ph2<<16);
          }
      }
      public void Color765bw2(int x,int y,int x2,int y2) {
        R.Norm(ref x,ref y,ref x2,ref y2);
				if(!IntersectRect(ref x,ref y,ref x2,ref y2,1,1,Width-2,Height-2)) return;
        if(x>x2||y>y2) return;
        int[] si=new int[766];
        int s,s2,i,a;
        for(int y3=y;y3<=y2;y3++) 
          for(int p=y3*Width+x,pe=p+x2-x;p<=pe;p++) {
            s=Palette.RGBSum(Data[p]);
            if(p+3<pe) {
              s2=Palette.RGBSum(Data[p+1]);
              if(s<s2) {i=s;a=s2;} else {i=s2;a=s;}
              if(i<a) {si[i]++;si[a]--;}
            }
            if(y3<y2) {
              s2=Palette.RGBSum(Data[p+Width]);
              if(s<s2) {i=s;a=s2;} else {i=s2;a=s;}
              if(i<a) {si[i]++;si[a]--;}
            }
          }
        for(i=0,s=0,a=765/2,s2=0;i<766;i++) {
          s+=si[i];
          if(s>s2) {a=i;s2=s;}
        }
        for(int y3=y;y3<=y2;y3++) 
          for(int p=y3*Width+x,pe=p+x2-x;p<=pe;p++)
            Data[p]=Palette.RGBSum(Data[p])<=a?0:White;

      }
      public void Color765grx(int x,int y,int x2,int y2,int lev,bool avg) {
        R.Norm(ref x,ref y,ref x2,ref y2);
				if(!IntersectRect(ref x,ref y,ref x2,ref y2,1,1,Width-2,Height-2)) return;
        if(x>x2||y>y2) return;
        int[] pi=new int[766],ni=new int[766],si=avg?new int[766]:null;
        int s,s2,i,a,e;
        for(int y3=y;y3<=y2;y3++) 
          for(int p=y3*Width+x,pe=p+x2-x;p<=pe;p++) {
            s=Palette.RGBSum(Data[p]);
            if(avg) si[s]++;
            if(p+3<pe) {
              s2=Palette.RGBSum(Data[p+1]);
              if(s<s2) {i=s;a=s2;} else {i=s2;a=s;}
              if(i<a) {pi[i]++;ni[a-1]++;}
            }
            if(y3<y2) {
              s2=Palette.RGBSum(Data[p+Width]);
              if(s<s2) {i=s;a=s2;} else {i=s2;a=s;}
              if(i<a) {pi[i]++;ni[a-1]++;}
            }
          }
        bool[] bi=new bool[766];
        for(int l=1;l<lev;l++) {
          for(i=0,s=0,a=-1,s2=0;i<766;i++) {
            s+=pi[i];
            if(s>s2) {a=i;s2=s;}
            s-=ni[i];
          }
          if(a<0) {
            lev=l;
            break;
          }
          bi[a+1]=true;          
          for(i=a-1,e=0,s=s2-pi[a]+ni[a];i>=0&&s>0;i--) {
            if(pi[i]>0) {              
              int sii=pi[i],es=Math.Min(e,sii);
              e-=es;sii-=es;
              es=Math.Min(sii,s);
              s-=es;pi[i]-=es;              
            }
            if(ni[i]>0) e+=ni[i];
          }
          for(i=a+1,e=0,s=s2;i<766&&s>0;i++) {
            if(ni[i]>0) {              
              int sii=ni[i],es=Math.Min(e,sii);              
              e-=es;sii-=es;
              es=Math.Min(sii,s);
              s-=es;ni[i]-=es;            
            }
            if(pi[i]>0) e+=pi[i];
          }
          pi[a]=ni[a]=0;
        }        
        for(i=a=s=0,e=0;i<766;i++) {
          if(avg) {
            if(i==e) {
              int j=i,n=0;s=0;
              do {
                n+=si[j];s+=j*si[j];
                if(bi[j++]) break;
              } while(j<766);
              s=(s/n)/3;
              //s=(j-1+i)/2/3;
              e=j;
            }
          }            
          pi[i]=s*0x10101;  
          if(!avg)
            if(bi[i]) {a++;s=a*255/(lev-1);}
        }
        for(int y3=y;y3<=y2;y3++) 
          for(int p=y3*Width+x,pe=p+x2-x;p<=pe;p++)
            Data[p]=pi[Palette.RGBSum(Data[p])];

      }
      static int gravg(int[] si,int f,int t) {
        int i,n=0,s=0;
        for(i=f;i<t;i++) {n+=si[i];s+=i*si[i];}
        return n>0?s/n:0;
      }
      static int grerr(int[] si,int f,int t) {
        int i,n=0,s=0,a;
        for(i=f;i<t;i++) {n+=si[i];s+=i*si[i];}
        if(n<1) return 0;
        a=s/n;
        for(i=f,s=0;i<a;i++) s+=(a-i)*si[i];
        for(i=a+1;i<t;i++) s+=(i-a)*si[i];
        return s;
      }
      int grerr2(int[] si,int f,int m,int t) {
        return grerr(si,f,m)+grerr(si,m,t);
      }
      public void Color765gro(int x,int y,int x2,int y2,int lev,bool avg) {
        R.Norm(ref x,ref y,ref x2,ref y2);
				if(!IntersectRect(ref x,ref y,ref x2,ref y2,1,1,Width-2,Height-2)) return;
        if(x>x2||y>y2) return;
        int[] si=new int[766];
        int[] ll=new int[lev+1];
        int s,i,j,l,chg;
        for(int y3=y;y3<=y2;y3++) 
          for(int p=y3*Width+x,pe=p+x2-x;p<=pe;p++) {
            s=Palette.RGBSum(Data[p]);
            si[s]++;
          }
        for(l=0;l<=lev;l++)
          ll[l]=(l*766/lev);
        j=0;
        do {
          chg=i=0;
          for(;i<lev-1&&j<4096;j++) {
            int li=ll[i],l1=ll[i+1],l2=ll[i+2];
            int e2,e=grerr2(si,li,l1,l2),mi=l1;
            for(l1=li+1;l1<l2;l1++) 
              if(si[l1]!=0) {
  	            e2=grerr2(si,li,l1,l2);
	              if(e2<e) {e=e2;mi=l1;}
              }
            //printf("%d. %d %d %d \n",j,i,l1,ll[i+1]);
            if(mi!=ll[i+1]) {
	            ll[i+1]=mi;
	            chg=1;
            }
            i++;
          }
        } while(chg==1);
        for(i=0,s=0,l=0;i<766;i++) {
          if(ll[l]==i) {s=gravg(si,ll[l],ll[l+1])/3;l++;}
          si[i]=s*0x10101;
        }
        for(int y3=y;y3<=y2;y3++) 
          for(int p=y3*Width+x,pe=p+x2-x;p<=pe;p++)
            Data[p]=si[Palette.RGBSum(Data[p])];

      }
      public void Color256x3(int x,int y,int x2,int y2,bool x3) {
        R.Norm(ref x,ref y,ref x2,ref y2);
				if(!IntersectRect(ref x,ref y,ref x2,ref y2,1,1,Width-2,Height-2)) return;
        if(x>x2||y>y2) return;
        int[] si=new int[x3?768:256];
        int c,c2,s,s2,i,j,a,sx,sd=x3?256:0,r,l0,l1,l2;
        for(int y3=y;y3<=y2;y3++) 
          for(int p=y3*Width+x,pe=p+x2-x;p<=pe;p++) {
            c=Data[p];
            if(p+1<pe) {
              c2=Data[p+1];
              for(sx=0,j=0;j<24;j+=8,sx+=sd) {
                i=(c>>j)&255;a=(c2>>j)&255;
                if(i>a) { r=i;i=a;a=r;}
                si[sx+i]++;si[sx+a]--;
              }
            }
            if(y3<y2) {
              c2=Data[p+Width];
              for(sx=0,j=0;j<24;j+=8,sx+=sd) {
                i=(c>>j)&255;a=(c2>>j)&255;
                if(i>a) { r=i;i=a;a=r;}
                si[sx+i]++;si[sx+a]--;
              }
            }
          }
        for(sx=0,j=0;j<(x3?3:1);j++,sx+=256) {
          for(i=0,s=0,a=127,s2=0;i<256;i++) {
            s+=si[sx+i];
            if(s>s2) {a=i;s2=s;}
          }
          si[j]=a;
        }           
        l0=si[0];
        if(x3) {l1=si[1];l2=si[2];} else {l1=l2=l0;}
        for(int y3=y;y3<=y2;y3++) 
          for(int p=y3*Width+x,pe=p+x2-x;p<=pe;p++) {
            c=Data[p];
            int c0=c&255,c1=(c>>8)&255;c2=(c>>16)&255;
            c0=c0<=l0?0:255;
            c1=c1<=l1?0:255;
            c2=c2<=l2?0:255;
            Data[p]=c0|(c1<<8)|(c2<<16);
          }
      }

      internal class xpal {
        internal int n,s0,s1,s2;
        internal byte i0,a0,i1,a1,i2,a2;        

      public override string ToString() {
        return ""+i0+"-"+a0+","+i1+"-"+a1+","+i2+"-"+a2;
      }
    }

      internal int xpal_dist(xpal[] map,int n,bool max,int c,out int ix,bool ps2) {
        byte c0=(byte)(c&255),c1=(byte)((c>>8)&255),c2=(byte)((c>>16)&255),x0,x1,x2;
        int m=766,d=0,r1,r2,s;
        ix=-1;
        for(int i=0;i<n;i++) {
          xpal mi=map[i];
          if(ps2) {
            x0=mi.i0;x1=mi.i1;x2=mi.i2;
            d=(c0>x0?c0-x0:x0-c0)+(c1>x1?c1-x1:x1-c1)+(c2>x2?c2-x2:x2-c2);
          } else {
            d=0;
            x0=mi.i0;x1=mi.a0;
            d+=x1-x0;
            if((s=x0-c0)>0) d+=s;else if((s=c0-x1)>0) d+=s;
            x0=mi.i1;x1=mi.a1;
            r1=x1-x0;
            if((s=x0-c1)>0) r1+=s;else if((s=c1-x1)>0) r1+=s;
            x0=mi.i2;x1=mi.a2;
            r2=x1-x0;
            if((s=x0-c2)>0) r2+=s;else if((s=c2-x1)>0) r2+=s;
            if(max) {
              if(r1>d) d=r1;
              if(r2>d) d=r2;
            } else 
              d+=r1+r2;
          }
          if(d<m) {m=d;ix=i;}
        }
        return m;
      }
      internal int xpal_join(xpal[] map,int n,bool max,ref int dist) {
        int i,im=-1,j,jm=-1,d,r1,r2,mi=766;
        byte i0,j0,i1,j1;
        xpal ib,jb;
  
        for(i=0;i<n-1;i++) {
          ib=map[i];
          for(j=i+1;j<n;j++) {
            jb=map[j];
            i0=ib.i0;i1=ib.a0;j0=jb.i0;j1=jb.a0;
            d=(j1>i1?j1:i1)-(j0<i0?j0:i0);
            i0=ib.i1;i1=ib.a1;j0=jb.i1;j1=jb.a1;      
            r1=(j1>i1?j1:i1)-(j0<i0?j0:i0);      
            i0=ib.i2;i1=ib.a2;j0=jb.i2;j1=jb.a2;      
            r2=(j1>i1?j1:i1)-(j0<i0?j0:i0);
            if(max) {
              if(r1>d) d=r1;
              if(r2>d) d=r2;
            } else 
              d+=r1+r2;
            if(d<mi) {mi=d;im=i;jm=j;}
          }
        }        
        ib=map[im];jb=map[jm];
        ib.n+=jb.n;
        ib.s0+=jb.s0;
        ib.s1+=jb.s1;
        ib.s2+=jb.s2;
        if(jb.i0<ib.i0) ib.i0=jb.i0;
        if(jb.a0>ib.a0) ib.a0=jb.a0;
        if(jb.i1<ib.i1) ib.i1=jb.i1;
        if(jb.a1>ib.a1) ib.a1=jb.a1;
        if(jb.i2<ib.i2) ib.i2=jb.i2;
        if(jb.a2>ib.a2) ib.a2=jb.a2;
        d=ib.a0-ib.i0+ib.a1-ib.i1+ib.a2-ib.i2;
        if(d>dist) dist=d;
        return jm;
      }

      public void MaxCount(int count,bool max,int x,int y,int x2,int y2) {
 			  R.Norm(ref x,ref y,ref x2,ref y2);
				if(!IntersectRect(ref x,ref y,ref x2,ref y2,0,0,Width-1,Height-1)) return;
        if(count<1||count>4096) return;
        xpal[] map=new xpal[count];        
        xpal xp;
        int n=0,dist=0,ix=0,c,d=1;
        for(int i=0;i<count;i++) map[i]=new xpal();
        for(int yi=y;yi<=y2;yi++)
            for(int xi=x,i=yi*Width+x,j;xi<=x2;xi++,i++) {
              c=Data[i]&White;              
              byte c0=(byte)(c&255),c1=(byte)((c>>8)&255),c2=(byte)((c>>16)&255);
              if(n>0) {
                c=c0|(c1<<8)|(c2<<16);
                if(n==count) {
                  xp=map[ix];                
                  if(c0<xp.i0||c0>xp.a0||c1<xp.i1||c1>xp.a1||c2<xp.i2||c2>xp.a2) ix=-1;
                  else d=dist;
                } else ix=-1;
                if(ix<0)
                  d=xpal_dist(map,n,max,c,out ix,false);
              }
              bool res;
              if((res=(n<count&&d>0)))
                ix=n++;
              else if((res=d>dist)) 
                ix=xpal_join(map,n,max,ref dist);
              if(res) {
                xp=map[ix];
                xp.n=xp.s0=xp.s1=xp.s2=0;
                xp.i0=xp.a0=c0;
                xp.i1=xp.a1=c1;
                xp.i2=xp.a2=c2;                
              }
              xp=map[ix];
              xp.n++;
              xp.s0+=c0;
              xp.s1+=c1;
              xp.s2+=c2;
              if(c0<xp.i0) xp.i0=c0;
              else if(c0>xp.a0) xp.a0=c0;
              if(c1<xp.i1) xp.i1=c1;
              else if(c1>xp.a1) xp.a1=c1;
              if(c2<xp.i2) xp.i2=c2;
              else if(c2>xp.a2) xp.a2=c2;
            }
          for(int i=0;i<n;i++) {
            xp=map[i];
            d=xp.n;
            byte c0=(byte)(xp.s0/d),c1=(byte)(xp.s1/d),c2=(byte)(xp.s2/d);
            xp.i0=c0;
            xp.i1=c1;
            xp.i2=c2;            
            xp.n=c0|(c1<<8)|(c2<<16);
          }
          for(int yi=y;yi<=y2;yi++)
            for(int xi=x,i=yi*Width+x;xi<=x2;xi++,i++) {
              c=Data[i]&White;              
              xpal_dist(map,n,max,c,out ix,true);                
              Data[i]=map[ix].n;
            }
      }

      public void hdr4(int x0,int y0,int x1,int y1,int size,bool rgb,bool satur,bool diag,int n0) {
        fillres res=new fillres() {x0=x0,y0=y0,x1=x1,y1=y1,m=0};
        if(!IntersectRect(ref x0,ref y0,ref x1,ref y1,1,1,Width-2,Height-2)) return;
        int y,x,j,k0=size<4?1:size/2,k,d,w2=x1-x0+1,h2=y1-y0+1,bpl=w2;
        bmap bm2=new bmap(this,x0,y0,x1,y1,0);
        int[] pix2=bm2.Data;
        if(size<1) size=2;
        for(y=size-1;y<h2-size;y++) {
         int g=(y0+y)*Width+x0+size-1,h=y*w2+size-1;
         for(x=2*size-2;x<w2;x++,h++,g++) {
           int c=pix2[h];
           byte c0=(byte)(c&255),c1=(byte)((c>>8)&255),c2=(byte)((c>>16)&255),mi0=c0,ma0=c0,mi1=c1,ma1=c1,mi2=c2,ma2=c2,cx;
           int n=n0,s0=n*c0,s1=n*c1,s2=n*c2,mi=c0+c1+c2,ma=mi,s=n*mi;
           for(k=1;k<size;k++) {
             if(diag) {
               int hh0=h-k*bpl,hh1=h+k*bpl,hh2=h+k,hh3=h-k;
               for(j=0;j<k;j++,hh0+=bpl+1,hh1+=-bpl-1,hh2+=bpl-1,hh3+=-bpl+1)
                 for(d=0;d<4;d++) {
                   int hh=d<2?d>0?hh1:hh0:d<3?hh2:hh3;
                   c=pix2[hh];
                   if(rgb) {
                     cx=(byte)(c&255);if(cx<mi0) mi0=cx;else if(cx>ma0) ma0=cx;
                     cx=(byte)((c>>8)&255);if(cx<mi1) mi1=cx;else if(cx>ma1) ma1=cx;
                     cx=(byte)((c>>16)&255);if(cx<mi2) mi2=cx;else if(cx>ma2) ma2=cx;
                   } else {
                     int z=Palette.RGBSum(c);if(z<mi) mi=z; else if(z>ma) ma=z;
                   }
                 }
             } else {
               int hh0=h+k+(1-k)*bpl,hh1=h-k*bpl-k,hh2=h+k*bpl+(1-k),hh3=h-k-k*bpl;
               for(j=0;j<2*k;j++,hh0+=bpl,hh1+=1,hh2+=1,hh3+=bpl)
                 for(d=0;d<4;d++) {
                   int hh=d<2?d>0?hh1:hh0:d<3?hh2:hh3;
                   c=pix2[hh];
                   if(rgb) {
                     cx=(byte)(c&255);if(cx<mi0) mi0=cx;else if(cx>ma0) ma0=cx;
                     cx=(byte)((c>>8)&255);if(cx<mi1) mi1=cx;else if(cx>ma1) ma1=cx;
                     cx=(byte)((c>>16)&255);if(cx<mi2) mi2=cx;else if(cx>ma2) ma2=cx;
                   } else {
                     int z=Palette.RGBSum(c);if(z<mi) mi=z; else if(z>ma) ma=z;
                   }
                 }
             }
             if(k<k0) continue;
              n++;
              if(rgb) {
                if(satur) {
                  int i=mi0<mi1?mi0:mi1,a=ma0>ma1?ma0:ma1;
                  if(mi2<i) i=mi2;if(ma2>a) a=ma2;
                  s0+=i==a?c0:(c0-i)*255/(a-i);
                  s1+=i==a?c1:(c1-i)*255/(a-i);
                  s2+=i==a?c2:(c2-i)*255/(a-i);
                } else {
                  s0+=mi0==ma0?c0:(c0-mi0)*255/(ma0-mi0);
                  s1+=mi1==ma1?c1:(c1-mi1)*255/(ma1-mi1);
                  s2+=mi2==ma2?c2:(c2-mi2)*255/(ma2-mi2);
                }
              } else {
                c=c0+c1+c2;
                s+=mi==ma?c:(c-mi)*765/(ma-mi);
              }
            }
          if(rgb) {
            Data[g]=(s0/n)|((s1/n)<<8)|((s2/n)<<16);
          } else {
            Data[g]=Palette.ColorIntensity765(pix2[h],s/n,satur);
          }
        }
      }}

     
      public void RemoveDots(bool black,bool white,char mode,bmap src) {
        Border(-1);
        ClearByte();
        src.ClearByte();
        int h=Height-2,w=Width-2,i=Width+1;
        bool all=!black&&!white;
        bool x3=mode=='3',x8=mode=='8',x4=!x3&&!x8;
        int[] data=src.Data;
        for(int y=0;y<h;y++,i+=2) 
          for(int x=0;x<w;x++,i++) {            
            if(black&&data[i]==0||white&&data[i]==White||all) {
              int c=data[i],c2=c;
              bool dot=data[i-1]!=c&&data[i+1]!=c&&data[i+Width]!=c&&data[i-Width]!=c;
              if(x8)
                 if(data[i-Width-1]==c||data[i-Width+1]==c||data[i+Width-1]==c||data[i+Width+1]==c) dot=false;              
              c=dot?Palette.RGBAvg4(data[i-1],data[i+1],data[i-Width],data[i+Width]):-1;
              if(!dot&&x3) {
                c=c2;
                bool l=data[i-1]!=c,r=data[i+1]!=c,u=data[i-Width]!=c,d=data[i+Width]!=c;
                if(l&&r&&(d||u)) c=Palette.RGBAvg3(data[i-1],data[i+1],data[i+(d?Width:-Width)]);
                else if(d&&u&&(l||r)) c=Palette.RGBAvg3(data[i-Width],data[i+Width],data[i+(r?1:-1)]);
              }
              if(c!=-1) Data[i]=c;
            }
          }
      }
      static byte[] bc16={0,1,1,2,1,2,2,3,1,2,2,3,2,3,3,4};
      public static int bitcount(int x) {
        int i,n;
        if(x==0) n=0;
        else if(x==-1) n=32;
        else for(i=n=0;i<32;i+=4) n+=bc16[(x>>i)&15];
        return n;
      }

      public int ColorCount(int x,int y,int x2,int y2) {
			  R.Norm(ref x,ref y,ref x2,ref y2);
				if(!IntersectRect(ref x,ref y,ref x2,ref y2,0,0,Width-1,Height-1)) return 0;
        if(x==x2&&y==y2) return 1;
        int[] map=new int[1<<19];
        int c,n=0;
        for(int yi=y;yi<=y2;yi++)
          for(int xi=x,i=yi*Width+x;xi<=x2;xi++,i++) {
            c=Data[i]&White;
            map[c>>5]|=1<<(c&31);
          }            
        foreach(int m in map) n+=bitcount(m);
        return n;
      }
      public void PalN(int n,bool avg,int x,int y,int x2,int y2) {
			  R.Norm(ref x,ref y,ref x2,ref y2);
				if(!IntersectRect(ref x,ref y,ref x2,ref y2,0,0,Width-1,Height-1)) return;
        if(n<1||n>64) return;
        int a=0,b=255/n+1;
        while(256+a<n*b) a++;
        int[] sum=null;
        if(avg) {
          sum=new int[4*n*n*n];
          for(int yi=y;yi<=y2;yi++)
            for(int xi=x,i=yi*Width+x,j;xi<=x2;xi++,i++) {
              int c=Data[i]&White;
              if(c==0||c==White) continue;
              int c0=c&255,c1=(c>>8)&255,c2=(c>>16)&255;
              if(n==2) j=(c0>127?1:0)+(c1>127?2:0)+(c2>127?4:0);            
              j=(c0+a)/b+n*((c1+a)/b+(c2+a)/b*n);
              j*=4;
              sum[j++]++;sum[j++]+=c0;sum[j++]+=c1;sum[j]+=c2;
            }
          for(int i=0,i4=0;i4<sum.Length;i++,i4+=4) {
            int s=sum[i4];
            if(s>0) sum[i]=sum[i4+1]/s+((sum[i4+2]/s)<<8)+((sum[i4+3]/s)<<16);
          }
        }
        for(int yi=y;yi<=y2;yi++)
          for(int xi=x,i=yi*Width+x,j;xi<=x2;xi++,i++) {
            int c=Data[i]&White;
            if(c==0||c==White) continue;
            int c0=c&255,c1=(c>>8)&255,c2=(c>>16)&255;
            if(avg) {
              if(n==2) j=(c0>127?1:0)+(c1>127?2:0)+(c2>127?4:0);            
              j=(c0+a)/b+n*((c1+a)/b+(c2+a)/b*n);
              Data[i]=sum[j];            
            } else {
              c0=(c0+a)/b*255/(n-1);
              c1=(c1+a)/b*255/(n-1);
              c2=(c2+a)/b*255/(n-1);
              Data[i]=c0+(c1<<8)+(c2<<16);
            }
          }
      }
      public void RemoveDust(int max,int color1,int color2,bool x8,int x,int y,int x2,int y2) {
			  R.Norm(ref x,ref y,ref x2,ref y2);
				if(!IntersectRect(ref x,ref y,ref x2,ref y2,0,0,Width-1,Height-1)) return;
        if(max<1||color1==color2) return;
        ClearByte();
        int[] q=new int[max+4];
        for(int yi=y;yi<=y2;yi++)
          for(int xi=x,i=yi*Width+x,h,xy;xi<=x2;xi++,i++) if(Data[i]==color1) {
            int n=0,m=1,nx,ny;
            q[n]=(xi&0xffff)|(yi<<16);Data[i]=color2;
            while(n<m&&m<=max) {
              xy=q[n++];nx=xy&0xffff;ny=xy>>16;
              h=Width*ny+nx;
              if(nx>x&&Data[h-1]==color1) {q[m++]=xy-1;Data[h-1]=color2;}
              if(nx<x2&&Data[h+1]==color1) {q[m++]=xy+1;Data[h+1]=color2;}
              if(ny>y&&Data[h-Width]==color1) {q[m++]=xy-0x10000;Data[h-Width]=color2;}
              if(ny<y2&&Data[h+Width]==color1) {q[m++]=xy+0x10000;Data[h+Width]=color2;}
              if(x8) {
                if(ny>y) {
                  if(nx>x&&Data[h-Width-1]==color1) {q[m++]=xy-0x10000-1;Data[h-Width-1]=color2;}
                  if(nx<x2&&Data[h-Width+1]==color1) {q[m++]=xy-0x10000+1;Data[h-Width+1]=color2;}
                }
                if(ny<y2) {
                  if(nx>x&&Data[h+Width-1]==color1) {q[m++]=xy+0x10000-1;Data[h+Width-1]=color2;}
                  if(nx<x2&&Data[h+Width+1]==color1) {q[m++]=xy+0x10000+1;Data[h+Width+1]=color2;}
                }
              }
            }
            bool go=m<max;
            if(!go) for(n=0;n<m;n++) {
              xy=q[n];nx=xy&0xffff;ny=xy>>16;
              h=Width*ny+nx;
              Data[h]=color1;
            }
          }
      }

      public void Bright(bool dark,bool bw,int level,int x,int y,int x2,int y2) {
			  R.Norm(ref x,ref y,ref x2,ref y2);
				if(!IntersectRect(ref x,ref y,ref x2,ref y2,0,0,Width-1,Height-1)) return;
        if(level<1||level>255) return;
				int i=y*Width+x,dx=x2-x+1;
				while(y<=y2) {
				  for(int ie=i+dx;i<ie;i++) {
						int c=Data[i];
            if(bw&&(c&White)==(dark?White:0)) continue;
						int b0=c&255,b1=(c>>8)&255,b2=(c>>16)&255;
						if(dark) {b0=b0*level/255;b1=b1*level/255;b2=b2*level/255;}
						else {b0=255-((255-b0)*level/255);b1=255-((255-b1)*level/255);b2=255-((255-b2)*level/255);}
						c=b0|(b1<<8)|(b2<<16);
						Data[i]=c;
					}
				  i+=Width-dx;
					y++;
				}
      }
      public void NoWhite(bool white,bool black) {
        if(!white&&!black) return;
        for(int i=0;i<Data.Length;i++) {
          int x=Data[i];
          if(white&&(x&White)==White) Data[i]=0xfefefe;
          else if(black&&(x&White)==Black) Data[i]=0x010101;
        }        
      }
      public void Contour(bool stroke,bool fill,bool black,int x,int y,int x2,int y2) {
        R.Norm(ref x,ref y,ref x2,ref y2);
        if(!IntersectRect(ref x,ref y,ref x2,ref y2,0,0,Width-1,Height-1)) return;
        int n=Data.Length-Width-1;
        ClearByte(x,y,x2,y2,0);
				for(int y3=y;y3<=y2;y3++)
				  for(int i=y3*Width+x,i0=i,ie=i+x2-x+1;i<ie;i++) {
            int ci=Palette.RGBSum(Data[i]);            
            if(black?i>0&&ci>Palette.RGBSum(Data[i-1])||i>ie-1&&ci<Palette.RGBSum(Data[i+1])||y3>y&&ci>Palette.RGBSum(Data[i-Width])||y3<y2&&ci>Palette.RGBSum(Data[i+Width])
                    :i>0&&ci<Palette.RGBSum(Data[i-1])||i<ie-1&&ci<Palette.RGBSum(Data[i+1])||y3>y&&ci<Palette.RGBSum(Data[i-Width])||y3<y2&&ci<Palette.RGBSum(Data[i+Width]))
              Data[i]|=c24;
          }
				for(int y3=y;y3<=y2;y3++)
				  for(int i=y3*Width+x,i0=i,ie=i+x2-x+1;i<ie;i++) 
            if(0!=(Data[i]&c24)) {
              if(stroke) Data[i]=black?White:Black;
            } else if(fill) Data[i]=black?Black:White;
      }
      public static int Blur(int c,int c0,int c1,int c2,int c3,int d0,int d1,int d2,int d3) {
        int r,g,b;
        r=(32*(c&255)+5*((c0&255)+(c1&255)+(c2&255)+(c3&255))+3*((d0&255)+(d1&255)+(d2&255)+(d3&255)))/64;
        g=((32*(c&0xff00)+5*((c0&0xff00)+(c1&0xff00)+(c2&0xff00)+(c3&0xff00))+3*((d0&0xff00)+(d1&0xff00)+(d2&0xff00)+(d3&0xff00)))/64)&0xff00;
        b=((32*(c&0xff0000)+5*((c0&0xff0000)+(c1&0xff0000)+(c2&0xff0000)+(c3&0xff0000))+3*((d0&0xff0000)+(d1&0xff0000)+(d2&0xff0000)+(d3&0xff0000)))/64)&0xff0000;
        return r|g|b|(c&(255<<24));
      }
      public void Filter(int x0,int y0,int x1,int y1,bool closed,int mode) {
        if(!IntersectRect(ref x0,ref y0,ref x1,ref y1,1,1,Width-2,Height-2)) return;
        if(x0>x1||y0>y1) return;
        int[] line=new int[2*Width];
        int g,w=x1-x0+1,x,idx;
        bool even=false;        
        for(int y=y0;y<=y1;y++) {
          g=even?Width:0;
          for(x=0,idx=y*Width+x0;x<w;x++,idx++) {
            int c=Data[idx],c0=Data[idx-1],c1=Data[idx+1],c2=Data[idx-Width],c3=Data[idx+Width];
            int d0=Data[idx-Width-1],d1=Data[idx-Width+1],d2=Data[idx+Width-1],d3=Data[idx+Width+1];
            if(closed) {
              if(x==0) c0=d0=d2=c;
              if(x==w-1) c1=d1=d3=c;
              if(y==y0) c2=d0=d1=c;
              if(y==y1) c3=d2=d3=c;
            }
            line[g++]=Blur(c,c0,c1,c2,c3,d0,d1,d2,d3);
          }
          g=even?0:Width;
          if(y>y0) for(x=0,idx=(y-1)*Width+x0;x<w;x++,idx++) Data[idx]=line[g+x];
          g=even?0:Width;
          even^=true;
        }
        g=even?0:Width;
        idx=y1*Width+x0;
        for(x=0;x<w;x++,idx++) Data[idx]=line[g+x];
      }
      public void RGB2CMY(bool inv) {
        if(Data!=null) for(int i=0;i<Data.Length;i++) Data[i]=Palette.RGB2CMY(Data[i],inv);
      }
      public void RGB2CMY(int x0,int y0,int x1,int y1,bool inv) {
        IntersectRect(ref x0,ref y0,ref x1,ref y1,0,0,Width-1,Height-1);
        while(y0<=y1) {
          int idx=y0*Width+x0;
          for(int x=x0;x<=x1;x++,idx++)
            Data[idx]=Palette.RGB2CMY(Data[idx],inv);
          y0++;
        }
      }
      public void RGBShift(int mode) {
        if(Data!=null) for(int i=0;i<Data.Length;i++) Data[i]=Palette.RGBShift(mode,Data[i]);
      }
      public void RGBShift(int mode,int x0,int y0,int x1,int y1) {
        IntersectRect(ref x0,ref y0,ref x1,ref y1,0,0,Width-1,Height-1);
        while(y0<=y1) {
          int idx=y0*Width+x0;
          for(int x=x0;x<=x1;x++,idx++)
            Data[idx]=Palette.RGBShift(mode,Data[idx]);          
          y0++;
        }
      }
      public void C4(int c00,int c01,int c10,int c11,int x0,int y0,int x1,int y1) {
        IntersectRect(ref x0,ref y0,ref x1,ref y1,0,0,Width-1,Height-1);        
        int y2=0,w=x1-x0,h=y1-y0;
        while(y0<=y1) {
          int idx=y0*Width+x0;
          int c0=Palette.RGBMix(c00,c10,y2,h),c1=Palette.RGBMix(c01,c11,y2,h);
          for(int x=x0,x2=0;x<=x1;x++,idx++,x2++) {
            int c=Palette.RGBMix(c0,c1,x2,w);
            Data[idx]=c;
/*            if(ccc>=0) {
              int y3=y2==0?0:y2*w/h;
              int scc=sqr(w/2-x2,w/2-y3);
              if(4*scc<sqr(w))
                c=Palette.RGBMix(ccc,c,2*isqrt(scc),w);
            }*/
           /*else {
            int y3=y2==0?0:y2*w/h;
            for(int x=x0,x2=0;x<=x1;x++,idx++,x2++) {
              int div=32;
              //int s00=256*w*h/(1+w*h/div+sqr(x2,y2*w/h)),s01=256*w*h/(1+w*h/div+sqr(w-x2,y2*w/h)),s10=256*w*h/(1+w*h/div+sqr(x2,w-y2*w/h)),s11=256*w*h/(1+w*h/div+sqr(w-x2,w-y2*w/h)),scc=256*w*h/(1+w*h/div+sqr(w/2-x2,w/2-y2*w/h));
              int s00=sqr(x2,y3);s00=4*s00<sqr(w,w)?sqr(w-isqrt(2*s00)):0;
              int s01=sqr(w-x2,y3);s01=4*s01<sqr(w,w)?sqr(w-isqrt(2*s01)):0;
              int s10=sqr(x2,w-y3);s10=4*s10<sqr(w,w)?sqr(w-isqrt(2*s10)):0;
              int s11=sqr(w-x2,w-y3);s11=4*s11<sqr(w,w)?sqr(w-isqrt(2*s11)):0;
              int scc=sqr(w/2-x2,w/2-y3);scc=4*scc<sqr(w,w)?sqr(w-isqrt(2*scc)):0;
              int s0=0,s1=0,s2=0;
              Palette.RGBAdd(c00,s00,ref s0,ref s1,ref s2);
              Palette.RGBAdd(c01,s01,ref s0,ref s1,ref s2);
              Palette.RGBAdd(c10,s10,ref s0,ref s1,ref s2);
              Palette.RGBAdd(c11,s11,ref s0,ref s1,ref s2);
              Palette.RGBAdd(ccc,scc,ref s0,ref s1,ref s2);
              Data[idx]=Palette.RGBAvg(s00+s01+s10+s11+scc,s0,s1,s2);*/
            /*int dy=abs(2*y2-h);
            for(int x=x0,x2=0;x<=x1;x++,idx++,x2++) {
              int dx=abs(2*x2-w),a,d,c0;
              if(dx*h<dy*w) {
                a=dy;d=h;c0=2*y2<h?Palette.RGBMix(c00,c01,w*dy+(2*x2-w)*h,2*w*dy):Palette.RGBMix(c10,c11,w*dy+(2*x2-w)*h,2*w*dy);
              } else {
                a=dx;d=w;c0=2*x2<w?Palette.RGBMix(c00,c10,h*dx+(2*y2-h)*w,2*h*dx):Palette.RGBMix(c01,c11,h*dx+(2*y2-h)*w,2*h*dx);
              }
              Data[idx]=Palette.RGBMix(ccc,c0,a,d);
            */            
          }
          y0++;y2++;
        }
      }

      public void Average(bool hor,bool ver,int x0,int y0,int x1,int y1) {
        if(!IntersectRect(ref x0,ref y0,ref x1,ref y1,0,0,Width-1,Height-1)) return;
        int[] v=ver&&hor?new int[3*(x1-x0+1)]:null;
        int idx,s0=0,s1=0,s2=0,nx=x1-x0+1,ny=y1-y0+1,c;
        if(ver||!hor) {          
          for(int x=x0;x<=x1;x++,idx++) { 
            idx=y0*Width+x;
            if(ver) s0=s1=s2=0;
            for(int y=y0;y<=y1;y++,idx+=Width)
              Palette.RGBAdd(Data[idx],ref s0,ref s1,ref s2);
            if(v!=null) { idx=3*(x-x0);v[idx++]=s0;v[idx++]=s1;v[idx++]=s2;}
            else if(ver)
              VLine(x,y0,y1,Palette.RGBAvg(ny,s0,s1,s2));
          }
        }
        if(!hor) {
          if(!ver) FillRectangle(x0,y0,x1,y1,Palette.RGBAvg(nx*ny,s0,s1,s2));
          return;
        }
        for(int y=y0;y<=y1;y++) {
          idx=y*Width+x0;
          if(hor) s0=s1=s2=0;
          for(int x=x0;x<=x1;x++,idx++) 
            Palette.RGBAdd(Data[idx],ref s0,ref s1,ref s2);
          if(v!=null) {
            idx=y*Width+x0;
            for(int x=x0,i=0;x<=x1;x++,idx++,i+=3)
              Data[idx]=Palette.RGBAvg(nx+ny,s0+v[i],s1+v[i+1],s2+v[i+2]);
          } else if(hor)
            HLine(x0,x1,y,Palette.RGBAvg(nx,s0,s1,s2));
        }
      }

      public void Pixels(int size,int x0,int y0,int x1,int y1) {
        if(size<2||!IntersectRect(ref x0,ref y0,ref x1,ref y1,0,0,Width-1,Height-1)) return;
        for(int y=y0,y2;y<=y1;y=y2) {
          y2=(y+size)/size*size;
          if(y2>y1+1) y2=y1+1;
          for(int x=x0,x2;x<=x1;x=x2) {
            x2=(x+size)/size*size;
            if(x2>x1+1) x2=x1+1;
            Average(false,false,x,y,x2-1,y2-1);
          }
        }
      }

      static readonly byte[] matrix={
    0,192,48,240,12,204,60,252,3,195,51,243,15,207,63,255,
    128,64,176,112,140,76,188,124,131,67,179,115,143,79,191,127,
    32,224,16,208,44,236,28,220,35,227,19,211,47,239,31,223,
    160,96,144,80,172,108,156,92,163,99,147,83,175,111,159,95,
    8,200,56,248,4,196,52,244,11,203,59,251,7,199,55,247,
    136,72,184,120,132,68,180,116,139,75,187,123,135,71,183,119,
    40,232,24,216,36,228,20,212,43,235,27,219,39,231,23,215,
    168,104,152,88,164,100,148,84,171,107,155,91,167,103,151,87,
    2,194,50,242,14,206,62,254,1,193,49,241,13,205,61,253,
    130,66,178,114,142,78,190,126,129,65,177,113,141,77,189,125,
    34,226,18,210,46,238,30,222,33,225,17,209,45,237,29,221,
    162,98,146,82,174,110,158,94,161,97,145,81,173,109,157,93,
    10,202,58,250,6,198,54,246,9,201,57,249,5,197,53,245,
    138,74,186,122,134,70,182,118,137,73,185,121,133,69,181,117,
    42,234,26,218,38,230,22,214,41,233,25,217,37,229,21,213,
    170,106,154,90,166,102,150,86,169,105,153,89,165,101,149,85,
  };
      public static int Matrix(int color,int x,int y,bool rgb,int level) {
        int off=((y&15)<<4)|(x&15),m=matrix[off];
        if(rgb) {
          int r=color&255,g=(color>>8)&255,b=(color>>16)&255;
          if(level<2) {          
            r=r>m||r==255?255:0;
            g=g>m||g==255?255:0;
            b=b>m||b==255?255:0;
          } else {
            r=Palette.Matrix(r,m,level);
            g=Palette.Matrix(g,m,level);
            b=Palette.Matrix(b,m,level);
          }
          return r|(g<<8)|(b<<16);
        } else {
          int s=Palette.RGBSum2(color);
          if(level<2)
            return s==765||s>3*m?White:Black;
          return 0x10101*Palette.Matrix((s+1)/3,m,level);
        }
      }
      public void Matrix(int x0,int y0,int x1,int y1,bool rgb,bool abs,int level) {
        IntersectRect(ref x0,ref y0,ref x1,ref y1,0,0,Width-1,Height-1);
        int y2=abs?y0:0;
        while(y0<=y1) {
          int idx=y0*Width+x0;
          for(int x=x0,x2=abs?x:0;x<=x1;x++,idx++,x2++)
            Data[idx]=Matrix(Data[idx],x2,y2,rgb,level);
          y0++;y2++;
        }
      }
      public void Diffuse(int x0,int y0,int x1,int y1,bool rgb,int level) {
        if(!IntersectRect(ref x0,ref y0,ref x1,ref y1,0,0,Width-1,Height-1)) return;
        int width=x1-x0,ph,ep,eb,eb0,eb1,eb2,e,e0,e1,e2,de,de3;
        if(rgb) {
          int[] emem=new int[3*(width+2)];
          while(y0<=y1) {
            ph=y0*Width+x0;
            ep=3;emem[0]=emem[1]=emem[2]=eb0=eb1=eb2=e0=e1=e2=0;
            for(int x=x0;x<=x1;x++,ph++,ep+=3) {
              int c=Data[ph],c0=c&255,c1=(c>>8)&255,c2=(c>>16)&255;
              e0+=emem[ep]+c0;e1+=emem[ep+1]+c1;e2+=emem[ep+2]+c2;
              if(level>2) {                
                c0=Palette.RGBLevel1(e0,level);e0-=c0;
                c1=Palette.RGBLevel1(e1,level);e1-=c1;
                c2=Palette.RGBLevel1(e2,level);e2-=c2;
              } else {
                if(e0>127) e0-=(c0=255);else c0=0;
                if(e1>127) e1-=(c1=255);else c1=0;
                if(e2>127) e2-=(c2=255);else c2=0;
              }
              Data[ph]=c0|(c1<<8)|(c2<<16);
              de3=e0/3;de=de3/3;
              emem[ep-3]+=de;emem[ep]=eb0+de3;
              e0-=de+de3+de;eb0=de;
              de3=e1/3;de=de3/3;
              emem[ep-2]+=de;emem[ep+1]=eb1+de3;
              e1-=de+de3+de;eb1=de;
              de3=e2/3;de=de3/3;
              emem[ep-1]+=de;emem[ep+2]=eb2+de3;
              e2-=de+de3+de;eb2=de;
            }
            y0++;
          }

        } else {
          int[] emem=new int[width+2];          
          while(y0<=y1) {
            ph=y0*Width+x0;
            ep=1;emem[0]=eb=e=0;
            for(int x=x0;x<=x1;x++,ph++,ep++) {
              int s=Palette.RGBSum(Data[ph]);
	            e+=emem[ep]+s;
	            if(e>382) {e-=765;s=White;} else s=0;
              Data[ph]=s;
	            de=e/9;
	            emem[ep-1]+=de;
	            de3=e/3;
	            emem[ep]=eb+de3;
	            e-=de+de3+de;
	            eb=de;
            }
            y0++;
          }
        }
      }
      public void Diffuse(int x0,int y0,int x1,int y1,int n,int[] pal) {
        if(!IntersectRect(ref x0,ref y0,ref x1,ref y1,0,0,Width-1,Height-1)) return;
        int width=x1-x0,ph,ep,eb0,eb1,eb2,e0,e1,e2,de,de3;
        int[] emem=new int[3*(width+2)];
          while(y0<=y1) {
            ph=y0*Width+x0;
            ep=3;emem[0]=emem[1]=emem[2]=eb0=eb1=eb2=e0=e1=e2=0;
            for(int x=x0;x<=x1;x++,ph++,ep+=3) {
              int c=Data[ph],c0=c&255,c1=(c>>8)&255,c2=(c>>16)&255;
              e0+=emem[ep]+c0;e1+=emem[ep+1]+c1;e2+=emem[ep+2]+c2;
              c=pal[Palette.search(e0,e1,e2,n,pal)];
              Data[ph]=c;
              e0-=c&255;e1-=(c>>8)&255;e2-=(c>>16)&255;              
              de3=e0/3;de=de3/3;emem[ep-3]+=de;emem[ep]=eb0+de3;e0-=de+de3+de;eb0=de;
              de3=e1/3;de=de3/3;emem[ep-2]+=de;emem[ep+1]=eb1+de3;e1-=de+de3+de;eb1=de;
              de3=e2/3;de=de3/3;emem[ep-1]+=de;emem[ep+2]=eb2+de3;e2-=de+de3+de;eb2=de;
            }
            y0++;
          }
      }
      public void Nearest(int x0,int y0,int x1,int y1,int n,int[] pal,int mode) {
        if(!IntersectRect(ref x0,ref y0,ref x1,ref y1,0,0,Width-1,Height-1)) return;
        while(y0<=y1) {
          for(int x=x0,idx=y0*Width+x0;x<=x1;x++,idx++)
            Data[idx]=pal[Palette.search(Data[idx],n,pal,mode)];
        }
      }
      
      public void Line(int x0,int y0,int x1,int y1,int color,bool whiteonly) {
        int r;
        if((x0<x1?x0>=Width||x1<0:x1>=Width||x0<0)||(y0<y1?y0>=Height||y1<0:y1>=Height||y0<0)) return;
        int dx=x1-x0,dy=y1-y0;
        if(dx<0) dx=-dx;if(dy<0) dy=-dy;
        int d=dx>dy?dx:dy;
        if(d==0) {XY(x0,y0,color,whiteonly);return;}
        dx=x1-x0;dy=y1-y0;
        for(int i=0;i<=d;i++) {
          int x=(d+2*(d*x0+i*dx))/d/2,y=(d+2*(d*y0+i*dy))/d/2;
          if(x>=0&&y>=0&&x<Width&&y<Height) {
            if(!whiteonly||(Data[y*Width+x]&0xffffff)==White)
              Data[y*Width+x]=color;
          }
        }
           
      }
/*      public int FillSide(int x,int y,int x0,int y0,int x1,int y1) {
        if(y0<y&&y1<y||y0>y&&y1>y||y0==y1) return 0;
        int x2=x0+(x1-x0)*(y-y0)/(y1-y0);
        return x2<x?y==y0?y1>y0?1:0:y==y1?y0>y1?1:0:1:0;
      }*/
      public void FillPath(List<int> path,int dx,int dy,int color,int cmode) {
        int[] p=new int[path.Count];
        for(int i=0;i<p.Length;i+=2) {p[i]=path[i]+dx;p[i+1]=path[i+1]+dy;}
        FillPath(p,color,cmode);
      }
      public void FillPath(int[] p,int color,int cmode) {
        int ix=p[0],iy=p[1],ax=p[0],ay=p[1];
        for(int i=0;i<p.Length;i+=2) {
          if(p[i]<ix) ix=p[i];else if(p[i]>ax) ax=p[i];
          if(p[i+1]<iy) iy=p[i+1];else if(p[i+1]>ay) ay=p[i+1];
        }
        if(ix>=Width||iy>=Height||ax<0||ay<0) return;
        if(ix<0) ix=0;if(iy<0) iy=0;
        if(ax>=Width) ax=Width-1;if(ay>=Height) ay=Height-1;
        int[] xa=new int[p.Length];
        bool[] ha=new bool[p.Length];
        for(int y=iy;y<=ay;y++) {
          int x1=p[p.Length-2],y1=p[p.Length-1],x2,y2;
          for(int i=0;i<p.Length;i+=2) {
            x2=x1;y2=y1;
            x1=p[i];y1=p[i+1];
            ha[i]=y1<y&&y2<y||y1>y&&y2>y||y1==y2;
            if(ha[i]) continue;
            int fx=x2-x1,fy=y2-y1;
            if(fx<0) fx=-fx;if(fy<0) fy=-fy;            
            int d=fx>fy?fx:fy,e=y2<y1?y1-y:y-y1;
            xa[i]=1+2*x1+2*(x2-x1)*e/fy;
          }        
          for(int x=ix;x<=ax;x++) {
            int n=0;
            x1=p[p.Length-2];y1=p[p.Length-1];
            for(int i=0;i<p.Length;i+=2) {
              x2=x1;y2=y1;
              x1=p[i];y1=p[i+1];
              if(ha[i]) continue;
              int r=xa[i]<=2*x?y==y1?y2>y1?1:0:y==y2?y1>y2?1:0:1:0;
              //int r=FillSide(x,y,x1,y1,x2,y2);
              n+=r;
            }
            if(0!=(n&1))
              Data[Width*y+x]=cmode>0?Palette.Colorize(cmode,color,Data[Width*y+x]):color;
          }
        }
      }
      public void BrushLine(int x0,int y0,int x1,int y1,int color,bmap brush,bool whiteonly) {
        BrushLine(x0,y0,x1,y1,color,brush,whiteonly,null,0);
      }
      public void BrushLine(int x0,int y0,int x1,int y1,int color,bmap brush,bool whiteonly,float[] dash,float dashoff) {
        if(brush==null) {
          if(dash==null) {
            Line(x0,y0,x1,y1,color,whiteonly);
            return;
          }
          brush=new bmap(1,1);
          brush.Data[0]=1;
        }
        int r;
        int bx=brush.Width/2,by=brush.Height/2;
        if((x0<x1?x0>=Width+brush.Width||x1<-brush.Width:x1>=Width+brush.Width||x0<-brush.Width)||(y0<y1?y0>=Height+brush.Height||y1<-brush.Height:y1>=Height+brush.Height||y0<-brush.Height)) return;
        int dx=x1-x0,dy=y1-y0;
        if(dx<0) dx=-dx;if(dy<0) dy=-dy;
        int d=dx>dy?dx:dy;
        if(d==0) {Brush(x0,y0,color,brush,whiteonly);return;}
        dx=x1-x0;dy=y1-y0;
        int patx=x0,paty=y0;
        float pattlen=dash==null?0:dash[dash.Length-1];
        for(int i=0;i<=d;i++) {
          //int x=x0+i*dx/d,y=y0+i*dy/d;
          int x=(d+2*(d*x0+i*dx))/d/2,y=(d+2*(d*y0+i*dy))/d/2;
          if(dash!=null) {
            float pi=(float)((dashoff+Math.Sqrt(sqr(x0-x,y0-y)))%pattlen);
            int p=0;
            while(p<dash.Length&&pi>=dash[p]) p++;
            if(0==(p&1))
              Brush(x,y,color,brush,whiteonly);
          } else
            Brush(x,y,color,brush,whiteonly);
        }
      }
      public void BrushQArc(int x0,int y0,int radius,bool px,bool py,bool vertical,int color,bmap brush,bool whiteonly) {
        int x1=x0+radius*(px?1:-1),y1=y0+radius*(py?-1:1);
        int bw=brush==null?1:brush.Width,bh=brush==null?1:brush.Height;
        int bx=bw/2,by=bh/2;
        if((x0<x1?x0>=Width+bw||x1<-bw:x1>=Width+bw||x0<-bw)||(y0<y1?y0>=Height+bh||y1<-bh:y1>=Height+bh||y0<-bh)) return;
        int dx=x1-x0,dy=y1-y0;
        if(dx<0) dx=-dx;if(dy<0) dy=-dy;
        int d=dx>dy?dx:dy;
        if(d==0) {Brush(x0,y0,color,brush,whiteonly);return;}
        dx=x1-x0;dy=y1-y0;
        for(int i=0;i<=2*radius;i++) {
          double a=i*Math.PI/radius/4,si=0.5+radius*Math.Sin(a),co=0.5+radius*Math.Cos(a);
          int x,y;
          if(vertical) {x=radius-(int)co;y=(int)si;} else {x=(int)si;y=radius-(int)co;}
          x=x0+x*(px?1:-1);y=y0+y*(py?1:-1);
          if(brush==null) XY(x,y,color,whiteonly);
          Brush(x,y,color,brush,whiteonly);
        }
      }
      public bmap Extend(int x0,int y0,int x1,int y1,int color,bool border) {
        R.Norm(ref x0,ref y0,ref x1,ref y1);
        if(x0>0) x0=0;if(y0>0) y0=0;
        if(x1<Width-1) x1=Width-1;if(y1<Height-1) y1=Height-1;
        if(x0==0&&y0==0&&x1==Width-1&&y1==Height-1) return null;        
        int w=x1-x0+1,h=y1-y0+1;
        if(w>16384||h>16384||w*h>64*1048576) return null;
        bmap dst=new bmap(w,h);
        dst.Clear(color);
        if(border) dst.CopyRectangle(this,1,1,Width-2,Height-2,1-x0,1-y0,-1);
        else dst.CopyRectangle(this,0,0,Width,Height,-x0,-y0,-1);
        return dst;        
      }
      public void Morph(bmap source,int[] pts) {
        int s1=sabs(pts[2]-pts[0],pts[3]-pts[1]),s2=sabs(pts[8]-pts[2],pts[9]-pts[3]);
        int s3=sabs(pts[6]-pts[4],pts[7]-pts[5]),s4=sabs(pts[0]-pts[6],pts[1]-pts[7]);
        int s5=sabs(pts[4]-pts[0],pts[5]-pts[1]),s6=sabs(pts[6]-pts[2],pts[7]-pts[3]);
        int d1=sabs(pts[10]-pts[8],pts[11]-pts[9]),d2=sabs(pts[12]-pts[10],pts[13]-pts[11]);
        int d3=sabs(pts[14]-pts[12],pts[15]-pts[13]),d4=sabs(pts[8]-pts[14],pts[9]-pts[15]);
        int d5=sabs(pts[12]-pts[8],pts[13]-pts[9]),d6=sabs(pts[14]-pts[10],pts[15]-pts[11]);                
        if(d3>d1) d1=d3;if(d4>d2) d2=d4;if(d6>d5) d5=d6;if(d3>d1) d1=d3;if(d5>d1) d1=d5;
        if(s3>s1) s1=s3;if(s4>s2) s2=s4;if(s6>s5) s5=s6;if(s3>s1) s1=s3;if(s5>s1) s1=s5;
        if(s1>d1) d1=s1;
        int n1=32,n=(d1+n1)&~(n1-1),n2=n/n1,n22=n2/2;
        int[] count=new int[Data.Length];
        for(int y0=0,y1=n;y1>=0;y0++,y1--)
          for(int x0=0,x1=n;x1>=0;x0++,x1--) {
            int dxa=(x1*pts[8]+x0*pts[10])/n1,dxb=(x1*pts[14]+x0*pts[12])/n1,dx=((y1*dxa+y0*dxb)/n+n22)/n2;
            int dya=(x1*pts[9]+x0*pts[11])/n1,dyb=(x1*pts[15]+x0*pts[13])/n1,dy=((y1*dya+y0*dyb)/n+n22)/n2;
            if(dx<0||dy<0||dx>=Width||dy>=Height) continue;
            int sxa=(x1*pts[0]+x0*pts[2])/n1,sxb=(x1*pts[6]+x0*pts[4])/n1,sx=(y1*sxa+y0*sxb)/n;
            int sya=(x1*pts[1]+x0*pts[3])/n1,syb=(x1*pts[7]+x0*pts[5])/n1,sy=(y1*sya+y0*syb)/n;
            dx=dy*Width+dx;
            int c=count[dx],color=source.Color(n2,sx,sy);
            if(color<0) continue;
            if(c>0) color=Palette.RGBMix(color,Data[dx],1,c);
            count[dx]=c+1;
            Data[dx]=color;
          }
      }
      public int Color(int div,int x,int y) {
        if(x<0||y<0) return -1;
        int px=x/div,py=y/div,cx=x-div*px,cy=y-div*py;
        if(px>=Width-1||py>=Height-1) return -1;
        int i=py*Width+px;
        int c0=Data[i],c1=Data[i+1],c2=Data[i+Width],c3=Data[i+Width+1];
        int dx=div-cx,dy=div-cy;
        int e0=dx*dy,e1=cx*dy,e2=dx*cy,e3=cx*cy;
        div=div*div;
        int r=(e0*(c0&255)+e1*(c1&255)+e2*(c2&255)+e3*(c3&255))/div;
        int g=(e0*((c0>>8)&255)+e1*((c1>>8)&255)+e2*((c2>>8)&255)+e3*((c3>>8)&255))/div;
        int b=(e0*((c0>>16)&255)+e1*((c1>>16)&255)+e2*((c2>>16)&255)+e3*((c3>>16)&255))/div;
        return (r&255)|(g<<8)|(b<<16);
      }
      public delegate int HalfFunc(int mode,int[] data,int[] wa,params int[] xa);
      public static int HalfAvg(int mode,int[] data,int[] wa,params int[] xa) {
        if(wa==null&&xa.Length==2) return Palette.RGBMix(data[xa[0]],data[xa[1]],wa==null?1:wa[1],wa==null?2:wa[0]+wa[1]);
        int i=0,s=0,s0=0,s1=0,s2=0,w;
        for(;i<xa.Length;i++) {
          s+=(w=wa==null?1:wa[i]);
          Palette.RGBAdd(data[xa[i]],w,ref s0,ref s1,ref s2);
        }
        return s==0?0:(s0/s)|((s1/s)<<8)|((s2/s)<<16);
      }
      public static int HalfMin(int mode,int[] data,int[] wa,params int[] xa) {
        bool max=0!=(mode&1);
        int i=0,s=max?0:255,s0=s,s1=s,s2=s;
        for(;i<xa.Length;i++)
          Palette.RGBMin(max,data[xa[i]],ref s0,ref s1,ref s2);
        return s0|(s1<<8)|(s2<<16);
      }
      public static int HalfIMin(int mode,int[] data,int[] wa,params int[] xa) {
        bool max=0!=(mode&1);
        int i=1,c=data[xa[0]];
        for(;i<xa.Length;i++)
          Palette.RGBIMin(max,data[xa[i]],ref c);
        return c;
      }
      public class HalfBack {
        public int a,b0,b1,b2;
        public HalfBack(int a,int b) { this.a=a;b0=b&255;b1=(b>>8)&255;b2=(b>>16)&255;}
        public int Func(int mode,int[] data,int[] wa,params int[] xa) {
          int i=0,s=0,s0=0,s1=0,s2=0;
          for(;i<xa.Length;i++) {            
            int c=data[xa[i]],c0=c&255,c1=(c>>8)&255,c2=(c>>16)&255,d0=Math.Abs(c0-b0),d1=Math.Abs(c1-b1),d2=Math.Abs(c2-b2);
            int w=a+d0+d1+d2;
            if(wa!=null) w*=wa[i];
            s+=w;s0+=w*c0;s1+=w*c1;s2+=w*c2;
          }
          s0/=s;s1/=s;s2/=s;
          return s0|(s1<<8)|(s2<<16);
        }
      }
      public bmap Half(bmap dst,bool vert,bool hori,HalfFunc f,int mode,int n) {
        if(!vert&&!hori) vert=hori=true;
        if(n<2) n=2;
        int w=Width/(hori?n:1),h=Height/(vert?n:1);
        int[] ia=new int[hori&&vert?n*n:n];
        if(dst==null) dst=new bmap(w,h);
        if(vert) {
          if(hori) {
            for(int y=0,y2=y,d=0,s=0;y<h;y++,d+=dst.Width,s+=n*Width)
              for(int x=0;x<w;x++) {
                for(int i=0,g=0,s2=s+n*x;i<n;i++,s2+=Width)
                  for(int j=0;j<n;j++)
                    ia[g++]=s2+j;
                dst.Data[d+x]=f(mode,Data,null,ia);
              }
          } else 
          for(int y=0,d=0,s=0;y<h;y++,d+=dst.Width,s+=n*Width)
            for(int x=0,s2=s;x<w;x++,s2++) {
              for(int i=0;i<n;i++) ia[i]=s2+i*Width;
              dst.Data[d+x]=f(mode,Data,null,ia);
            } 
        } else if(hori)
          for(int y=0,d=0,s=0;y<h;y++,d+=dst.Width,s+=Width)
            for(int x=0,s2=s;x<w;x++,s2+=n) {
              for(int i=0;i<n;i++) ia[i]=s2+i;
              dst.Data[d+x]=f(mode,Data,null,ia);
            }
        return dst;
      }
      public bmap Half23(bmap dst,bool vert,bool hori,HalfFunc f,int mode) {
        if(!vert&&!hori) vert=hori=true;
        int w=Width,h=Height;
        if(hori) w=w/3*2;if(vert) h=h/3*2;
        int[] ia=new int[hori&&vert?4:2],wa=new int[ia.Length];
        if(wa.Length==2) {wa[0]=2;wa[1]=1;} else {wa[0]=4;wa[1]=2;wa[2]=2;wa[3]=1;}
        if(dst==null) dst=new bmap(w,h);
        if(vert) {
          if(hori) {
            for(int y=0,y2=y,d=0,s=0;y<h;y+=2,d+=2*dst.Width,s+=3*Width)
              for(int x=0,s2=s;x<w;x+=2,s2+=3) {
                ia[0]=s2;ia[1]=s2+1;ia[2]=s2+Width;ia[3]=ia[2]+1;
                dst.Data[d+x]=f(mode,Data,wa,ia);
                ia[0]=s2+2;ia[2]=s2+2+Width;
                dst.Data[d+x+1]=f(mode,Data,wa,ia);
                ia[0]=s2+2*Width+2;ia[1]=ia[0]-1;
                dst.Data[d+dst.Width+x+1]=f(mode,Data,wa,ia);
                ia[0]=s2+2*Width;ia[2]=s2+Width;
                dst.Data[d+dst.Width+x]=f(mode,Data,wa,ia);
              }
          } else 
          for(int y=0,d=0,s=0;y<h;y+=2,d+=2*dst.Width,s+=3*Width)
            for(int x=0,s2=s;x<w;x++,s2++) {
              ia[0]=s2;ia[1]=s2+Width;
              dst.Data[d+x]=f(mode,Data,wa,ia);
              ia[0]=s2+2*Width;
              dst.Data[d+x+dst.Width]=f(mode,Data,wa,ia);
            } 
        } else if(hori)
          for(int y=0,d=0,s=0;y<h;y++,d+=dst.Width,s+=Width)
            for(int x=0,s2=s;x<w;x+=2,s2+=3) {
              ia[0]=s2;ia[1]=s2+1;
              dst.Data[d+x]=f(mode,Data,wa,ia);
              ia[0]=s2+2;
              dst.Data[d+x+1]=f(mode,Data,wa,ia);
            }
        return dst;
      }
      public bmap Half34(bmap dst,bool vert,bool hori,HalfFunc f,int mode) {
        if(!vert&&!hori) vert=hori=true;
        int w=Width,h=Height;
        if(hori) w=w/4*3;if(vert) h=h/4*3;
        int[] ia=new int[hori&&vert?4:2],wa=new int[ia.Length],wa2=null;
        if(hori&&vert) {
          wa[0]=9;wa[1]=3;wa[2]=3;wa[3]=1;
          wa2=new int[4];wa2[0]=3;wa2[1]=3;wa2[2]=1;wa2[3]=1;          
        } else {wa[0]=3;wa[1]=1;}
        if(dst==null) dst=new bmap(w,h);
        if(vert) {
          if(hori) {
            for(int y=0,y2=y,d=0,s=0;y<h;y+=3,d+=3*dst.Width,s+=4*Width)
              for(int x=0,s2=s;x<w;x+=3,s2+=4) {
                for(int i=0,r;i<4;i++) {
                  if(i==0) {r=0;ia[0]=s2;ia[1]=ia[0]+1;ia[2]=ia[0]+Width;ia[3]=ia[2]+1;}
                  else if(i==1) {r=2;ia[0]=s2+3;ia[1]=ia[0]-1;ia[2]=ia[0]+Width;ia[3]=ia[2]-1;}
                  else if(i==2) {r=2*dst.Width;ia[0]=s2+3*Width;ia[1]=ia[0]+1;ia[2]=ia[0]-Width;ia[3]=ia[2]+1;}
                  else {r=2*dst.Width+2;ia[0]=s2+3*Width+3;ia[1]=ia[0]-1;ia[2]=ia[0]-Width;ia[3]=ia[2]-1;}
                  dst.Data[d+x+r]=f(mode,Data,wa,ia);

                  if(i==0) {r=1;ia[0]=s2+1;ia[1]=ia[0]+1;ia[2]=ia[0]+Width;ia[3]=ia[2]+1;}
                  else if(i==1) {r=dst.Width;ia[0]=s2+Width;ia[1]=ia[0]+1;ia[2]=ia[0]+Width;ia[3]=ia[2]+1;}
                  else if(i==2) {r=dst.Width+2;ia[0]=s2+Width+3;ia[1]=ia[0]-1;ia[2]=ia[0]+Width;ia[3]=ia[2]-1;}
                  else {r=2*dst.Width+1;ia[0]=s2+3*Width+1;ia[1]=ia[0]+1;ia[2]=ia[0]-Width;ia[3]=ia[2]+1;}
                  dst.Data[d+x+r]=f(mode,Data,wa2,ia);
                }
               
                ia[0]=s2+Width+1;ia[1]=ia[0]+1;ia[2]=ia[0]+Width;ia[3]=ia[2]+1;
                dst.Data[d+dst.Width+x+1]=f(mode,Data,null,ia);
              }
          } else 
          for(int y=0,d=0,s=0;y<h;y+=3,d+=3*dst.Width,s+=4*Width)
            for(int x=0,s2=s;x<w;x++,s2++) {
              ia[0]=s2;ia[1]=s2+Width;
              dst.Data[d+x]=f(mode,Data,wa,ia);
              ia[0]=s2+2*Width;
              dst.Data[d+x+dst.Width]=f(mode,Data,null,ia);
              ia[0]=s2+3*Width;ia[1]=s2+2*Width;
              dst.Data[d+x+2*dst.Width]=f(mode,Data,wa,ia);
            } 
        } else if(hori)
          for(int y=0,d=0,s=0;y<h;y++,d+=dst.Width,s+=Width)
            for(int x=0,s2=s;x<w;x+=3,s2+=4) {
              ia[0]=s2;ia[1]=s2+1;
              dst.Data[d+x]=f(mode,Data,wa,ia);
              ia[0]=s2+2;
              dst.Data[d+x+1]=f(mode,Data,null,ia);
              ia[0]=s2+3;ia[1]=s2+2;
              dst.Data[d+x+2]=f(mode,Data,wa,ia);
            }
        return dst;
      }
     }
				public struct PathPoint {
		  public int x,y;
			public bool stop,fill;
      public byte shape;
			public PathPoint(int X,int Y,bool Stop) { x=X;y=Y;stop=Stop;shape=0;fill=false;}
			public PathPoint(int X,int Y) { x=X;y=Y;stop=false;shape=0;fill=false;}
      public void Set(string f) {
        foreach(char ch in f)
          if(ch=='b') shape=2;
          else if(ch=='c') shape=1;
          else if(ch=='t') shape=3;
          else if(ch=='d') shape=4;
          else if(ch=='h') shape=6;
          else if(ch=='f') fill=true;
      }
			public PointPath List() { PointPath l=new PointPath();l.Add(this);return l;}
		  public override string ToString() { return ""+x+","+y+(stop?",O":"")+"";}
	  }
    public class FillPattern {
      public bmap BMap;
      public int X,Y,TrColor;
      public bool MX,MY,HX,HY;
      public bool Enabled;

      public FillPattern(int trcolor) { TrColor=trcolor;}

      public int Color(int x,int y) {
         int rx,ry,rc;
         x-=X;y-=Y;
         ry=bmap.modp(y,2*BMap.Height);
         rx=bmap.modp(x,2*BMap.Width);
         if(HX&&ry>=BMap.Height) { rx+=BMap.Width/2;if(rx>=2*BMap.Width) rx-=2*BMap.Width;}
         if(HY&&rx>=BMap.Width) { ry+=BMap.Height/2;if(ry>=2*BMap.Height) ry-=2*BMap.Height;}
         if(rx>=BMap.Width) rx=MX?2*BMap.Width-1-rx:rx-BMap.Width;
         if(ry>=BMap.Height) ry=MY?2*BMap.Height-1-ry:ry-BMap.Height;
         rc=BMap.Data[BMap.Width*ry+rx]&bmap.White;
         return rc==TrColor?-1:rc;
      }
    }
		public class PointPath {
		  public PathPoint[] pt=new PathPoint[16];
			public int Count;
			public bool Closed;
			public void Add(int x,int y) { Add(new PathPoint(x,y));}
			public void AddU(PathPoint pp) { 
        if(Count==0||pt[Count-1].x!=pp.x||pt[Count-1].y!=pp.y||pt[Count-1].stop!=pp.stop)
          Add(pp);
      }
			public void Add(PathPoint pp) {
			  if(Count==pt.Length) Array.Resize(ref pt,Count*2);
				pt[Count++]=pp;
			}
			public void SetStop() {if(Count>0) { pt[Count-1].stop=true;			  
			  } else Closed=true;
			}
      public void Set(string f) { if(Count>0) pt[Count-1].Set(f);}

			public PathPoint this[int index] { get {return pt[index];} set {pt[index]=value;}}
			public void Clear() {Count=0;Closed=false;}
      public int X { get {return Count<1?int.MaxValue:pt[0].x;}}
      public int Y { get {return Count<1?int.MaxValue:pt[0].y;}}

		}

    public static class R {
      public static int[] Copy(int[] rect) { return rect==null?null:rect.Clone() as int[];}
      public static int Width(int[] rect) { return Math.Abs(rect[2]-rect[0])+1;}
      public static int Height(int[] rect) { return Math.Abs(rect[3]-rect[1])+1;}
      public static bool IsPoint(int[] rect) { return rect[0]==rect[2]&&rect[1]==rect[3];}
      public static bool Intersected(int[] rect,int x,int y,int x2,int y2) {
        return !(x>rect[2]||x2<rect[0]||y>rect[3]||y2<rect[1]);
      }      
      public static void Union(int[] rect,int x,int y,int x2,int y2) {
        if(x<rect[0]) rect[0]=x;if(x2>rect[2]) rect[2]=x2;
        if(y<rect[1]) rect[1]=y;if(y2>rect[3]) rect[3]=y2;
      }
      public static void Union(int[] rect,int[] r) { Union(rect,r[0],r[1],r[2],r[3]);}
      public static bool Union(ref int l,ref int t,ref int r,ref int b,int x,int y) { return Union(ref l,ref t,ref r,ref b,x,y,x,y);}
      public static bool Union(ref int l,ref int t,ref int r,ref int b,int x,int y,int x2,int y2) {
        if(x<l) l=x;if(x2>r) r=x2;
        if(y<t) t=y;if(y2>b) b=y2;
        return true;
      }      
      public static bool Intersect(int[] rect,int x,int y,int x2,int y2) {
        if(x>rect[2]||x2<rect[0]||y>rect[3]||y2<rect[1]) return false;
        if(x>rect[0]) rect[0]=x;if(x2<rect[2]) rect[2]=x2;
        if(y>rect[1]) rect[1]=y;if(y2<rect[3]) rect[3]=y2;
        return true;
      }
      public static bool Intersect(ref int x,ref int y,ref int x2,ref int y2,int xa,int ya,int xb,int yb) {
        if(x>xb||x2<xa||y>yb||y2<ya) return false;
        if(xa>x) x=xa;if(xb<x2) x2=xb;
        if(ya>y) y=ya;if(yb<y2) y2=yb;        
        return true;
      }
      public static void Norm(int[] rect) {
        if(rect==null) return;
        int r;
        if(rect[0]>rect[2]) {r=rect[0];rect[0]=rect[2];rect[2]=r;}
        if(rect[1]>rect[3]) {r=rect[1];rect[1]=rect[3];rect[3]=r;}
      }
      public static void Norm(ref int x,ref int y,ref int x2,ref int y2) {
        int r;
        if(x>x2) {r=x;x=x2;x2=r;}
        if(y>y2) {r=y;y=y2;y2=r;}
      }      
			public static bool Inside(int[] rect,int x,int y) {			  
			  return rect[0]<=x&&x<=rect[2]&&rect[1]<=y&&y<=rect[3];
			}
			public static bool Inside(int[] rect,int x,int y,int x2,int y2) {			  
			  return rect[0]>=x&&rect[2]<=x2&&rect[1]>=y&&rect[3]<=y2;
			}
			public static void Shift(int[] rect,int dx,int dy) {
        if(rect==null) return;
			  rect[0]+=dx;rect[1]+=dy;
				rect[2]+=dx;rect[3]+=dy;
			}

    }
}
