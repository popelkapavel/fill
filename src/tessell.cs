using System;
using System.Drawing;

//#define GRC

namespace fill {

    public class Tess {
      public int Height,Start;
      public int[] XN;

      public Tess(int height) { XN=new int[2*(Height=height)];}
      public Tess(int height,int[] xn,int start) { Height=height;XN=xn;Start=start;}
      public Tess(Tess src) {Copy(src);}
      public Tess Clone() { return new Tess(this);}
      public Tess Ghost() { return new Tess(Height,XN,Start);}
      public void Copy(Tess src) {
        if(src==null) return;
        XN=src.XN.Clone() as int[];
        Start=src.Start;Height=src.Height;
      }
      public void Alloc(int height) {        
        XN=new int[2*(Height=height)];
      }
      public void FMima(int bpl,int[] data,int start) {
        if(Height<1) return;
        int p,pe,c,q,qe,x=Start,mi=data[start+XN[x+1]]&255,ma=mi,b,sum=0;
        for(q=start,qe=q+Height*bpl;q<qe;q+=bpl,x+=2)
          for(p=q+XN[x+1],pe=p+XN[x];p<pe;) 
            Palette.RGBMinMax(data[p++],ref mi,ref ma,ref sum);
        ma-=mi;
        if(ma==255||ma==0) return;
        for(x=Start,q=start,qe=q+Height*bpl;q<qe;q+=bpl,x+=2)
          for(p=q+XN[x+1],pe=p+XN[x];p<pe;p++) {
            data[p]=Palette.RGBMima(data[p],mi,ma);
          }
      }
      public void FMima3(int bpl,int[] data,int start) {
        if(Height<1) return;
        int x=Start,p=start+XN[x+1],pe,c,q,qe,mi0,mi1,mi2,ma0,ma1,ma2,b,sum=0;
        mi0=ma0=data[p]&255;mi1=ma1=(data[p]>>8)&255;mi2=ma2=(data[p]>>16)&255;
        for(q=start,qe=q+Height*bpl;q<qe;q+=bpl,x+=2)
          for(p=q+XN[x+1],pe=p+XN[x];p<pe;) 
            Palette.RGBMinMax(data[p++],ref mi0,ref mi1,ref mi2,ref ma0,ref ma1,ref ma2);
        ma0-=mi0;ma1-=mi1;ma2-=mi2;
        if(ma0==255) ma0=0;
        if(ma1==255) ma1=0;
        if(ma2==255) ma2=0;
        if((ma0|ma1|ma2)==0) return;
        for(x=Start,q=start,qe=q+Height*bpl;q<qe;q+=bpl,x+=2)
          for(p=q+XN[x+1],pe=p+XN[x];p<pe;p++) {
            data[p]=Palette.RGBMima(data[p],mi0,mi1,mi2,ma0,ma1,ma2);
          }
      }

      public static Tess Dia1(int size,int[] dxy) {
        int n=2*size-1,h=0,g=2*(n-1),i;
        Tess t=new Tess(n);
        int[] xn=t.XN;
        for(i=1;i<=size;i++,h+=2,g-=2) {
          xn[g+1]=xn[h+1]=size-i;xn[g]=xn[h]=2*i-1;          
        }
        dxy[0]=n;dxy[1]=-1;dxy[2]=size;dxy[3]=size-1;
        return t;
      }
      public static Tess Dia2(int size,int[] dxy) {
        int n=2*size,h=0,g=2*(n-1),i;
        Tess t=new Tess(n);
        int[] xn=t.XN;
        for(i=1;i<=size;i++,h+=2,g-=2) {
          xn[g+1]=xn[h+1]=size-i;xn[g]=xn[h]=2*i;
        }
        dxy[0]=n;dxy[1]=0;dxy[2]=size;dxy[3]=size+1;
        return t;
      }
      public static Tess Dia(int size,int[] dxy) {
        int n=2*size-1,h=0,g=2*(n-1),i;
        Tess t=new Tess(n);
        int[] xn=t.XN;
        for(i=1;i<size;i++,h+=2,g-=2) {
          xn[h]=2*i-1;xn[g+1]=xn[h+1]=size-i;xn[g]=2*i;
        }
        xn[h]=n;xn[h+1]=0;
        dxy[0]=size;dxy[1]=size;dxy[2]=1-size;dxy[3]=size;
        return t;
      }
      public static Tess Box(int w,int h,int[] dxy) {
        int p=0,i;
        Tess t=new Tess(h);
        int[] xn=t.XN;
        for(i=0;i<h;i++,p+=2) {
          xn[p]=w;xn[p+1]=0;
        }
        dxy[0]=w;dxy[1]=0;dxy[2]=0;dxy[3]=h;
        return t;
      }
      public static Tess Tria(int w,int h,int[] dxy) {
        int p=0,i=0,o=w&1,c=(w+1)/2,d;
        Tess t=new Tess(h);
        int[] xn=t.XN;
        for(i=0;i<h;i++,p+=2) {
          d=(c*(i+1)+(h/2))/h;
          if(d<1) d=1;
          xn[p]=2*d-o;xn[p+1]=c-d;
        }
        dxy[0]=xn[0]+xn[2*h-2];dxy[1]=0;dxy[2]=c;dxy[3]=h;
        return t;
      }
      public static Tess Hex(int size,int[] dxy) {
        int i,j,kj,k=size*bmap.isqrt(3*128*128)/256,s2=size/2,h,g;
        Tess t=Box(2*k,2*size,dxy);
        if(s2<1||k<1) return t;
        int[] xn=t.XN;
        for(i=1,h=0,g=h+2*(t.Height-s2);i<=s2;i++,h+=2,g+=2) {
          j=i*k/s2;kj=k-j;
          xn[h]=2*j;xn[h+1]=kj;
          xn[g]=2*kj;xn[g+1]=j;
        }
        dxy[0]=2*k;dxy[1]=0;dxy[2]=k;dxy[3]=3*s2;
        return t;
      }
      public int DY1(int c,int a,int min) {
        int h,i=Height,b=a+c-1,e,f;
        for(h=Start+2*(i-1);i>0&&i>min;h-=2,i--) {
          e=XN[h+1];f=e+XN[h]-1;
          if(e<=f&&!(e>b||f<a)) return i;
        }
        return i;
      }
      public int DY(Tess m,int x) {
        int r=0,i=0,j,h=m.Start;
        for(;i<m.Height&&r+i<Height;i++,h+=2) {
          j=DY1(m.XN[h],x+m.XN[h+1],r+i)-i;
          if(j>r) r=j;
        }
        return r;        
      }
      public static Tess Circle(int size,int[] dxy) {
        int i,j,n,k=size,ij,s2=k*k,h,g;
        Tess t=Box(2*k,2*size,dxy);
        if(k<2) return t;
        int[] xn=t.XN;
        for(i=k-1,h=0,g=2*(t.Height-1);i>=0;i--,h+=2,g-=2) {
          j=bmap.isqrt(s2-i*i);
          ij=i*i+j*j;
          if(ij<s2) j++;
          if(j<1) j=1;
          xn[g]=xn[h]=2*j;
          xn[g+1]=xn[h+1]=k-j;
        }
        dxy[0]=2*k;dxy[1]=0;dxy[2]=k;dxy[3]=t.DY(t,k);
        return t;
      }

      public int Stat(int[] s) {
        int a,b,c,d,n=Height,x=Start;
        while(n>0&&XN[x]==0) {n--;x+=2;}
        if(n<1) return 0;
        c=XN[x];a=XN[x+1];b=a+c;d=1;
        s[0]=s[1]=a;s[2]=s[3]=b;s[4]=s[5]=c;
        do {
          x+=2;
          if(--n==0) return d;
          c=XN[x];a=XN[x+1];b=a+c;d++;
          if(a<s[0]) s[0]=a;else if(a>s[1]) s[1]=a;
          if(b<s[2]) s[2]=b;else if(b>s[3]) s[3]=b;
          if(c<s[4]) s[4]=c;else if(c>s[5]) s[5]=c;
          d++;
        } while(true);
      }      
      public bool BBox(out int min,out int max) { return BBox(Height,XN,Start,out min,out max);}
      public static bool BBox(int height,int[] xn,int start,out int min,out int max) {
        int mi,ma,a,b,n=height,x=start;
        while(n>0&&xn[x]==0) {n--;x+=2;}        
        if(n<1) {min=max=0;return false;}
        for(mi=xn[x+1],ma=mi+xn[x],x+=2;--n>0;x+=2)
          if((b=xn[x])>0) {
            if((a=xn[x+1])<mi) mi=a;
            if((b+=a)>ma) ma=b;
          }
        min=mi;max=ma;
        return true;
    }    
    public void XAdd(int dn,int dx) {
      int h=Start,he=h+2*Height;
      for(;h<he;h+=2)
        if(XN[h]>0) {XN[h]+=dn;XN[h+1]+=dx;}
    }
    public void YInv() {
      int h=Start,p=h+2*Height-2,a,b;
      for(;h<p;h+=2,p-=2)
        {a=XN[h];b=XN[h+1];XN[h]=XN[p];XN[h+1]=XN[p+1];XN[p]=a;XN[p+1]=b;}
    }
    public void XInv(int c) {
      int h=Start,he=h+2*Height;
      for(;h<he;h+=2) XN[h+1]=2*c-(XN[h+1]+XN[h]);
    }
    public int Dua() {
      int[] st=new int[6];
      int h=Start,he=h+2*Height,a,b,c,i,m;
      if(Stat(st)<1) return 0;
      m=st[4]+st[5];
      for(;h<he;h+=2) {
        c=XN[h];a=XN[h+1];b=a+c;
        XN[h]=m-c;XN[h+1]=b;
      }
      return m;
    }

    public int YClip(int y0,int y1) { return YClip(ref Height,ref Start,y0,y1);}
    public static int YClip(ref int height,ref int start,int y0,int y1) {
      int n=height;
      if(y0>=n||y1<1) return height=0;
      if(n>y1) n=y1;
      if(y0>0) {n-=y0;start+=2*y0;} else y0=0;
      height=n<0?0:n;
      return y0;
    }
    public int XClip(int[] buff,int x0,int x1) { return XClip(ref Height,ref XN,ref Start,buff,x0,x1);}
    public static int XClip(ref int height,ref int[] xn,ref int start,int[] buff,int x0,int x1) {
      int h=start,g,a,b,n=height,n2;
      for(n2=n;n2!=0;n2--,h+=2) {
        b=xn[h];a=xn[h+1];
        if((b=xn[h])!=0&&((a=xn[h+1])<x0||x1<a+b)) {
          Array.Copy(xn,start,buff,0,2*(n-n2));          
          for(g=h-start;n2!=0;n2--,h+=2,g+=2) {
            a=xn[h+1];b=a+xn[h];
            if(a<x0) a=x0;if(x1<b) b=x1;
            b-=a;
            if(b>0) {buff[g]=b;buff[g+1]=a;} else buff[g]=buff[g+1]=0;
          }
          xn=buff;
          return 0;
        }
      }
      return 0;
    }
    public static int Rota(int height,int[] xn,int start,out int[] res,int[] dxy) {
      int mi,ma,i,j,p,q;
      if(!BBox(height,xn,start,out mi,out ma)) { res=null;return 0;}
      res=new int[2*(ma-=mi)];
      for(i=ma,q=0;i--!=0;q+=2) {res[q]=0;res[q+1]=height;}
      for(i=0,p=0;i<height;i++,p+=2)
        for(j=xn[p],q=2*(xn[p+1]-mi);j--!=0;q+=2) {
          if(i<res[q+1]) res[q+1]=i;
          if(i+1>res[q]) res[q]=i+1;
        }
      for(i=ma,q=0;i--!=0;q+=2) if((j=res[q])!=0) res[q]=j-res[q+1];
      if(dxy!=null) {
        i=dxy[0];dxy[0]=dxy[1];dxy[1]=i;
        i=dxy[2];dxy[2]=dxy[3];dxy[3]=i;
      }
      return ma;
    }
    public bool Rota(int[] dxy) {
      int[] res;
      int h;
      h=Rota(Height,XN,Start,out res,dxy);
      if(h<1||XN==null) return false;
      Height=h;XN=res;Start=0;
      return true;
    }

    public static void Tessel(int width,int height,int bpl,int[] data,int start,int size,int shape,bool mima3) {
      int xi,xa,i,j;
      int[] dxy=new int[4],xn3;
      Tess t,t2;
      if(size<1) size=16;
      if(shape==3||shape==4) {t=Tess.Box(2*size,2*size,dxy);if(shape==4) dxy[2]=size;}
      else if(shape==2) t=Tess.Tria(size,size,dxy);
      else if(shape==1||shape==5) {t=Tess.Hex(size,dxy);if(shape==5) t.Rota(dxy);}
      else if(shape==6) {t=Tess.Dia(size,dxy);dxy[2]-=size/2;dxy[3]-=size/2;}
      else if(shape==7) t=Tess.Circle(size,dxy);
      else t=Tess.Dia2(size,dxy);
      xn3=new int[2*t.Height];
     tria:
      t.BBox(out xi,out xa);
      for(j=-100;j<100;j++) {
        for(i=-100;i<100;i++) {
           int dx=i*dxy[0]+j*dxy[2],dy=i*dxy[1]+j*dxy[3];
           if(dx<=-xa||dx>=width-xi||dy<=-t.Height||dy>=height) continue;
           t2=t.Ghost();
           if(dy<0||dy+t2.Height>height) dy+=Tess.YClip(ref t2.Height,ref t2.Start,-dy,height-dy);
           if(dx+xi<0||dx+xa>width) dy+=t2.XClip(xn3,-dx,width-dx);
           if(t2.Height>0) {
            if(mima3) t2.FMima3(bpl,data,start+dx+dy*bpl);
            else t2.FMima(bpl,data,start+dx+dy*bpl);
           }
         }
      }
      if(shape==2) {
        shape=0;
        t.XAdd(0,t.XN[0]+t.XN[1]);
        t.YInv();
        goto tria;
      }
    }
  }
}
