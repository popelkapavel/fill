using System;
using System.Drawing;

//#define GRC

namespace fill {

    public class IntCmp {
      public delegate bool Func(int x);
      public int Color;
      public IntCmp(int x) { Color=x;}
      public bool EQ(int x) { return x==Color;}
      public bool NE(int x) { return x!=Color;}
      public bool LT(int x) { return x<Color;}
      public bool LE(int x) { return x<=Color;}
      public bool GT(int x) { return x>Color;}
      public bool GE(int x) { return x>=Color;}
      public bool NEP(int x) { return x!=Color&&x>=0;}
    }
    public static class Palette {
      public static int[] pal16={0x0,0xffffff,0xff0000,0x00ff00,0x0000ff,0x00ffff,0xff00ff,0xffff00
        ,0x808080,0x800000,0x008000,0x000080,0x008080,0x800080,0x008080,0xc0c0c0};

        public static double max(double a,double b,double c) {
          return a>b?a>c?a:c:b>c?b:c;  
        }
        public static double size(double a,double b,double c) {
          return Math.Sqrt(a*a+b*b+c*c);
        }
        public static int abs(int x) { return x<0?-x:x;}
        public static int abs(int x,int y) { return x>=y?x-y:y-x;}
        public static int min(int x,int y) { return x<=y?x:y;}
        public static int max(int x,int y) { return x>=y?x:y;}
        public static int sqr(int x) { return x*x;}
        public static int range(int x,int min,int max) { return x<min?min:x>max?max:x;}
        public static int search(int e0,int e1,int e2,int n,int[] pal) {
          int i=0,mi=-1,e,m=0xffffff;
          for(;i<n;i++) {
            int p=pal[i],p0=p&255,p1=(p>>8)&255,p2=(p>>16)&255;
            e=sqr(e0-p0)+sqr(e1-p1)+sqr(e2-p2);
            if(e<m) {m=e;mi=i;}
          }
          return mi;
        }
        public static int search(int c,int n,int[] pal,int mode) {
          int i=0,mi=-1,e,m=0xffffff,c0=c&255,c1=(c>>8)&255,c2=(c>>16)&255;
          for(;i<n;i++) {
            int p=pal[i],p0=p&255,p1=(p>>8)&255,p2=(p>>16)&255;
            if(mode==2) e=sqr(c0-p0)+sqr(c1-p1)+sqr(c2-p2);
            else if(mode==3) e=max(max(abs(c0,p0),abs(c1,p1)),abs(c2,p2));
            else e=abs(c0,p0)+abs(c1,p1)+abs(c2,p2);
            if(e<m) {m=e;mi=i;}            
          }
          return mi;
        }
        public static int RGBMix8(int color1,int color2,int i) {
          if(i<=0) return color1;
          if(i>=256) return color2;
          int result=color1+((((color2&0xff)-(color1&0xff))*i)>>8)
              +((((((color2>>8)&0xff)-((color1>>8)&0xff))*i)>>8)<<8)
              +((((((color2>>16)&0xff)-((color1>>16)&0xff))*i)>>8)<<16);
          return result;
        }
        public static int RGBMix(int color1,int color2,int i,int max,int gammax) {
          if(gammax!=0) {int m=max;i=(int)((max=1<<23)*Gammax(gammax,i/(double)m));}
          if(i<=0) return color1;
          if(i>=max) return color2;
          return RGBMix(color1,color2,i,max);
        }
        public static int RGBMix(int color1,int color2,int i,int max) {
          if(i<=0) return color1;
          if(i>=max) return color2;
          int result,c0=color1&255,c1=(color1>>8)&255,c2=(color1>>16)&255;
          result=color1+((color2&255)-c0)*i/max
            +(((((color2>>8)&255)-c1)*i/max)<<8)
            +(((((color2>>16)&255)-c2)*i/max)<<16);
          /* result=((color2&255)*i+(color1&255)*(max-i))/max
            |(((((color2>>8)&255)*i+((color1>>8)&255)*(max-i))/max)<<8)
            |(((((color2>>16)&255)*i+((color1>>16)&255)*(max-i))/max)<<16);*/
          return result;
        }
        public static int RGBMix(int[] pal,int i,int max) {
          if(i<=0) return pal[0];
          int n=pal.Length-1;          
          if(i>=max||n<1) return pal[n];
          int ni=i*n,a=ni/max,an=a*max;
          return RGBMix(pal[a],pal[a+1],ni-an,max);
        }
        public static int RGBMin(int a,int b,bool rgb) {
          if(rgb) {
            byte a0=(byte)(a&255),a1=(byte)((a>>8)&255),a2=(byte)((a>>16)&255),bx;
            bx=(byte)(b&255);if(bx<a0) a0=bx;
            bx=(byte)((b>>8)&255);if(bx<a1) a1=bx;
            bx=(byte)((b>>16)&255);if(bx<a2) a2=bx;
            return a0|(a1<<8)|(a2<<16);
          } else 
            return RGBSum(b)<RGBSum(a)?b:a;
        }
        public static int RGBMax(int a,int b,bool rgb) {
          if(rgb) {
            byte a0=(byte)(a&255),a1=(byte)((a>>8)&255),a2=(byte)((a>>16)&255),bx;
            bx=(byte)(b&255);if(bx>a0) a0=bx;
            bx=(byte)((b>>8)&255);if(bx>a1) a1=bx;
            bx=(byte)((b>>16)&255);if(bx>a2) a2=bx;
            return a0|(a1<<8)|(a2<<16);
          } else 
            return RGBSum(b)>RGBSum(a)?b:a;
        }
        public static byte RGBMin(byte a,byte b,byte c) { if(b<a) a=b;return a<c?a:c;}
        public static byte RGBMax(byte a,byte b,byte c) { if(b>a) a=b;return a>c?a:c;}
        public static int RGBDiff(int color1,int color2) {
          return abs(color1&255,color2&255)|(abs((color1>>8)&255,(color2>>8)&255)<<8)|(abs((color1>>16)&255,(color2>>16)&255)<<16);
        }
        public static int RGBDiff(int color1,int color2,int mul) {
          int c0=mul*abs(color1&255,color2&255),c1=mul*abs((color1>>8)&255,(color2>>8)&255),c2=mul*abs((color1>>16)&255,(color2>>16)&255);
          if(c0>255) c0=255;
          if(c1>255) c1=255;
          if(c2>255) c2=255;
          return c0|(c1<<8)|(c2<<16);

        }
        public static int RGBSqr(int color1,int color2) {
          int d0=(color1&255)-(color2&255),d1=((color1>>8)&255)-((color2>>8)&255),d2=((color1>>16)&255)-((color2>>16)&255);
          return d0*d0+d1*d1+d2*d2;
        }
        public static int RGBSqrt(int color1,int color2,int mul) {
          int s=bmap.isqrt(RGBSqr(color1,color2));
          s=mul*s*255/441;
          if(s>255) s=255;
          return 0x10101*s;
        }
        /*public static bool RGBDiff(int mode,int diff,int c,int c2) {
          return !RGBEqual(mode,diff,c,c2);
          /*int r=c&255,g=(c>>8)&255,b=(c>>16)&255;
          int d=r-(c2&255),e=g-((c2>>8)&255),f=b-((c2>>16)&255);
          if(d<0) d=-d;if(e<0) e=-e;if(f<0) f=-f;
          if(mode==1) return d+e+f<3*diff;
          if(mode==2) return d*d+e*e+f*f<diff*diff;
          int l=mode<1?d<e?d<f?d:f:e<f?e:f:d>e?d>f?d:f:e>f?e:f;
          return l<diff;
        }*/
        public static bool RGBDiff(int mode,int level,int color1,int color2) {
          if(mode==6) {
            int h1=RGBHue(color1,false,0),h2=RGBHue(color2,false,0);
            int g0=abs(h1,h2),g1=abs(h1+1536,h2);
            return (g0<g1?g0:g1)>level;
          }  
          int c0=color1&255,c1=(color1>>8)&255,c2=(color1>>16)&255;
          int e0=color2&255,e1=(color2>>8)&255,e2=(color2>>16)&255;
          if(mode==7) {
            int ci,ca,ei,ea;
            if(c0<c1) {ci=c0;ca=c1;} else {ci=c1;ca=c0;}
            if(c2<ci) ci=c2;else if(c2>ca) ca=c2;
            if(e0<e1) {ei=e0;ea=e1;} else {ei=c1;ea=e0;}
            if(e2<ei) ei=e2;else if(e2>ea) ea=e2;
            return abs(e0,e1)>level;
          }
          if(mode==4) return abs(c0+c1+c2,e0+e1+e2)>level;          
          int d0=abs(c0,e0),d1=abs(c1,e1),d2=abs(c2,e2);
          if(mode==0) return (d0<d1?d0<d2?d0:d2:d1<d2?d1:d2)>level;
          if(mode==+3) return (d0>d1?d0>d2?d0:d2:d1>d2?d1:d2)>level;
          if(mode==2) return d0*d0+d1*d1+d2*d2>level*level;
          if(mode==5) return d0+d1+d2>3*level;
          return d0+d1+d2>level;
        }
        public static int RGBMin(int color1,int color2) {
          return min(color1&255,color2&255)|(min((color1>>8)&255,(color2>>8)&255)<<8)|(min((color1>>16)&255,(color2>>16)&255)<<16);
        }
        public static int RGBMax(int color1,int color2) {
          return max(color1&255,color2&255)|(max((color1>>8)&255,(color2>>8)&255)<<8)|(max((color1>>16)&255,(color2>>16)&255)<<16);
        }
        public static int RGBEmbo(int color1,int color2,int mul) {
          int c0=128+mul*((color1&255)-(color2&255)),c1=128+mul*(((color1>>8)&255)-((color2>>8)&255)),c2=128+mul*(((color1>>16)&255)-((color2>>16)&255));
          if(c0<0) c0=0;else if(c0>255) c0=255;
          if(c1<0) c1=0;else if(c1>255) c1=255;
          if(c2<0) c2=0;else if(c2>255) c2=255;
          return c0|(c1<<8)|(c2<<16);     
        }
        public static int RGBXmix(int color1,int color2) {
          if(((color2^color1)&bmap.White)==0) return color1;
          int c0=(((color1>>16)&255+(color1)&255))/2;
          int c1=(((color1>>8)&255+(color2>>8)&255))/2;
          int c2=(((color2>>16)&255+(color2)&255))/2;
          return c0|(c1<<8)|(c2<<16);
        }
        public static int RGBAvg(int n,int s0,int s1,int s2) {
          return n<1?0:((s0/n)&255)|(((s1/n)&255)<<8)|(((s2/n)&255)<<16);
        }
        public static int RGBAvg2(int n,int s0,int s1,int s2) {
          int m=n*255;
          if(s0<0) s0=0;else if(s0>m) s0=m;else s0/=n;
          if(s1<0) s1=0;else if(s1>m) s1=m;else s1/=n;
          if(s2<0) s2=0;else if(s2>m) s2=m;else s2/=n;
          return n<1?0:(s0&255)|((s1&255)<<8)|((s2&255)<<16);
        }
        public static int RGBAvg4(int c1,int c2,int c3,int c4) {
          int r=(c1&0xff)+(c2&0xff)+(c3&0xff)+(c4&0xff);
          int g=(c1&0xff00)+(c1&0xff00)+(c3&0xff00)+(c4&0xff00);
          int b=(c1&0xff0000)+(c1&0xff0000)+(c3&0xff0000)+(c4&0xff0000);
          return ((r>>2)&0xff)|((g>>2)&0xff00)|((b>>2)&0xff0000);
        }
        public static int RGBAvg3(int c1,int c2,int c3) {
          int r=(c1&0xff)+(c2&0xff)+(c3&0xff);
          int g=(c1&0xff00)+(c1&0xff00)+(c3&0xff00);
          int b=(c1&0xff0000)+(c1&0xff0000)+(c3&0xff0000);
          return ((r/3)&0xff)|((g/3)&0xff00)|((b/3)&0xff0000);
        }
        public static int RGBAvg(int[] data,int offset,int stride,int from,int to) {
          int i=from>>8,ie=to>>8;
          if(i>=ie) return data[offset+i*stride];
          int w,s=256-(from&255),s0=0,s1=0,s2=0;
          RGBAdd(data[offset+i*stride],s,ref s0,ref s1,ref s2);
          s+=w=to&255;
          if(w>0) RGBAdd(data[offset+ie*stride],w,ref s0,ref s1,ref s2);
          while(++i<ie) {
            RGBAdd(data[offset+i*stride],256,ref s0,ref s1,ref s2);
            s+=256;
          }
          return RGBAvg(s,s0,s1,s2);          
        }
        public static int RGBMap(int c,byte[] map) {
          int r=c&255,g=(c>>8)&255,b=(c>>16)&255;
          return (c&(255<<24))|map[r]|(map[g]<<8)|(map[b]<<16);
        }
        public static int RGBMapic(int c,int[] map) {
          int r=c&255,g=(c>>8)&255,b=(c>>16)&255;
          return map[r+g+b];
        }
        public static int RGBMapii(int c,int[] map,bool satur) {
          int r=c&255,g=(c>>8)&255,b=(c>>16)&255;
          int i=map[r+g+b];
          return ColorIntensity765(c,i,satur);
        }
        public static void Color(byte[] data,int offset,double value,double[] palette,bool hsv) {         
          int p;
          if(value<=palette[0]) {
            data[offset+2]=(byte)(palette[1]*255.5);
            data[offset+1]=(byte)(palette[2]*255.5);
            data[offset]=(byte)(palette[3]*255.5);
          } else if(value>=palette[palette.Length-4]) {
            p=palette.Length-3;
            data[offset+2]=(byte)(palette[p++]*255.5);
            data[offset+1]=(byte)(palette[p++]*255.5);
            data[offset]=(byte)(palette[p++]*255.5);
          } else {
            for(p=0;p<palette.Length&&value>palette[p+4];p+=4);
            double r1=(value-palette[p])/(palette[p+4]-palette[p]),r0=1-r1;
            if(hsv) {
              double r=palette[p+1]*r0+palette[p+5]*r1;
              double g=palette[p+2]*r0+palette[p+6]*r1;
              double b=palette[p+3]*r0+palette[p+7]*r1;
              double s=size(palette[p+1],palette[p+2],palette[p+3])*r0+size(palette[p+5],palette[p+6],palette[p+7])*r1;
              double s2=size(r,g,b);
              if(s2>0) {
                double m=max(r,g,b);
                s/=s2;
                if(m*s>1) s=1/m;
                r*=s;g*=s;b*=s;
              }
              data[offset+2]=(byte)(255.5*r);
              data[offset+1]=(byte)(255.5*g);
              data[offset]=(byte)(255.5*b);
            } else {
              data[offset+2]=(byte)(255.5*(palette[p+1]*r0+palette[p+5]*r1));
              data[offset+1]=(byte)(255.5*(palette[p+2]*r0+palette[p+6]*r1));
              data[offset]=(byte)(255.5*(palette[p+3]*r0+palette[p+7]*r1));
            }
          }
        }
        public static void RGB2HSV(double r,double g,double b,out double h,out double s,out double v) {
           double min=r<g?r<b?r:b:g<b?g:b;
           double max=r>g?r>b?r:b:g>b?g:b;
           v=max;
           if(max==min) {
             s=0;
             h=-1;
             return;
           }
           double delta=max-min;
           s=delta/max;
           if(r==max) h=(g-b)/delta;
           else if(g==max) h=2+(b-r)/delta;
           else h=4+(r-g)/delta;
           if(h<0) h+=6;
           //h*=60;
        }
        public static void HSV2RGB(double h,double s,double v,out double r,out double g,out double b) {
          if(s==0) {
            r=g=b=v;
            return;
          }
          //h/=60;
          int i=(int)Math.Floor(h);
          double f=h-i;
          double p=v*(1-s),q=v*(1-s*f),t=v*(1-s*(1-f));
          switch(i) {
           case 0:r=v;g=t;b=p;break;
           case 1:r=q;g=v;b=p;break;
           case 2:r=p;g=v;b=t;break;
           case 3:r=p;g=q;b=v;break;
           case 4:r=t;g=p;b=v;break;
           default:r=v;g=p;b=q;break;               
          }          
        }
        public static int ColorIntensity(int color,int i) {
          if(i==100) return color;
          if(i<=0) return 0;
          int r=color&255,g=(color>>8)&255,b=(color>>16)&255;
          r=r*i/256;if(r>255) r=255;
          g=g*i/256;if(g>255) g=255;
          b=b*i/256;if(b>255) b=255;
          return r|(g<<8)|(b<<16);
        }
        public static double Gammax(int gammax,double v) {
          byte gx=(byte)(gammax&0xf);
          if(0!=(gammax&0xf0)) {
            int n=((gammax>>4)&0xf)+1,m;
            m=(int)Math.Floor(v*=n);
            v-=m;
            if(0!=(m&1)) v=1-v;
          }
          if(v<=0) v=0;
          else if(v>=1) v=1;
          else if(gx==1) v*=v;
          else if(gx==2) { v=1-v;v=1-v*v;}
          else if(gx==3) { if(v>0.5) { v=2*(1-v);v*=v;v=1-v/2;} else { v=4*v*v/2;}}
          else if(gx==4) { if(v>0.5) { v=2*(v-0.5);v=(v*v+1)/2;} else { v=(1-2*v);v=(1-v*v)/2;}}
          if(0!=(gammax&0xf00)&&v>0&&v<1) {
            int n=((gammax>>8)&0xf)+1,m;
            m=(int)Math.Floor(n*v);
            v=(double)m/(n-1);
          }
          return v;
        }
        public static double Gamma(double gamma,double value) {
          return gamma==0?1-Math.Sqrt(1-value*value):Math.Pow(value,gamma);
        }
        public static int Gamma(int color,double gamma,bool mode) {
          int r=color&255,g=(color>>8)&255,b=(color>>16)&255;
          bool up=gamma>1;
          if(up) {r=255-r;g=255-g;b=255-b;}
          else gamma=1/gamma;
          if(mode) {
            r=(int)(r/gamma+0.49);g=(int)(g/gamma+0.49);b=(int)(b/gamma+0.49);
          } else {
            r=(int)(255*Math.Pow(r/255.0,gamma)+0.49);
            g=(int)(255*Math.Pow(g/255.0,gamma)+0.49);
            b=(int)(255*Math.Pow(b/255.0,gamma)+0.49);
          }
          if(up) {r=255-r;g=255-g;b=255-b;};
          return r|(g<<8)|(b<<16);
        }
        public static byte[] Gamma(bool inv,double gamma) {
          byte[] m=new byte[256];
          byte xor=(byte)(inv?255:0);
          for(int i=1;i<255;i++) {
            double f=Gamma(gamma,i/255.0);
            m[i^xor]=(byte)(xor^(int)(255*f+0.49));
          }
          m[255]=255;
          return m;
        }
        public static int[] Gammai(bool inv,double gamma) {
          int[] m=new int[766];
          for(int i=1;i<765;i++) {
            double f=Gamma(gamma,i/765.0);
            int v=(int)(765*f+0.49);
            m[inv?765-i:i]=inv?765-v:v;            
          }
          m[765]=765;
          return m;
        }
        public static int Matrix(int x,int m,int level) {
          if(level<2) return x>m||x==255?255:0;
          if(x==255||x==0) return x;
          int a=x*level,b=a/256;
          x=a&255;
          if(x>m) b++;
          return b*255/level;
        }
        public static int ColorIntensity765(int color,int i) { return ColorIntensity765(color,i,false);}
        public static int ColorIntensity765(int color,int i,bool satur) {
          if(i<=0) return bmap.Black;else if(i>=765) return bmap.White;
          int r=color&255,g=(color>>8)&255,b=(color>>16)&255;
          return ColorIntensity765(r,g,b,i,satur?0:-1);
        }
        public static int ColorIntensity765(int r,int g,int b,int i,int satur) {
          int s=r+g+b;
          if(s==i) goto end;
          if(r==b&&b==g) { r=g=i/3;b=i-r-g;goto end2;}
          if(satur>0) {
            if(i<s) {
              int mi=r<g?r:g;
              if(b<mi) mi=b;
              if(mi>0) {
                if(satur>1)
                  {r=255-(255-r)*255/(255-mi);g=255-(255-g)*255/(255-mi);b=255-(255-b)*255/(255-mi);}
                else {
                  if(3*mi>s-i) mi=(s-i)/3;
                  r-=mi;g-=mi;b-=mi;
                }                  
              }
            } else {
              int ma=r>g?r:g;
              if(b>ma) ma=b;
              if(ma<255) {
                if(satur>1)
                  {r=r*255/ma;g=g*255/ma;b=b*255/ma;}
                else {
                  ma^=255;
                  if(3*ma>i-s) ma=(i-s)/3;
                  r+=ma;g+=ma;b+=ma;
                }                  
              }            
            }
            s=r+g+b;
          }
          if(i<s) {
            if(i+2<s) {r=r*i/s;g=g*i/s;}
            b=i-r-g;
          } else {
            i=765-i;s=765-s;
            if(i+2<s) {r=255-((255-r)*i/s);g=255-((255-g)*i/s);}
            b=(765-i)-r-g;
          }
         end2:
          if(b<0) {
            b++;if(r>0) r--;else if(g>0) g--;
            if(b<0) {b++;if(g>0) g--;else if(r>0) r--;}
          } else if(b>255) {
            b--;if(r<255) r++;else if(g<255) g++;
            if(b>255) {b--;if(g<255) g++;else if(r<255) r++;}
          }         
         end: 
          return r|(g<<8)|(b<<16);
        }
        public static int SetIntensity(int c0,int c1,int c2,int i,bool satur) {
          int sum=c0+c1+c2;
          if(sum==i) return c0|(c1<<8)|(c2<<16);
          if(i<sum) return 0xffffff^AddIntensity765(c0,c1,c2,sum-i);
          else return AddIntensity765(c0,c1,c2,i-sum);
        }
        public static int AddIntensity765(int c0,int c1,int c2,int add) {
          int ma;
          if(c0>c1) ma=c0;else ma=c1;
          if(c2>ma) ma=c2;
          int avg;
          if(ma<255) {
            ma=255-ma;
            avg=add/3;
            if(avg>ma) avg=ma;
            c0+=avg;c1+=avg;c2+=avg;
            add-=3*avg;
          }
          if(add>0) {
            int n=(c0<255?1:0)+(c1<255?1:0)+(c2<255?1:0);
            if(n==1) {
              if(c0<255) c0+=add;
              else if(c1<255) c1+=add;
              else if(c2<255) c2+=add;
            } else {
              if(n==2) {
                int x=255-c0+255-c1+255-c2,add2=add;
                avg=(255-c0)*add2/x;c0+=avg;add-=avg;
                avg=(255-c1)*add2/x;c1+=avg;add-=avg;
                avg=(255-c2)*add2/x;c2+=avg;add-=avg;
              }
              if(add>0&&c0<255) {c0++;add--;}
              if(add>0&&c1<255) {c1++;add--;}
              if(add>0&&c2<255) {c2++;add--;}
            }
          }          
          return c0|(c1<<8)|(c2<<16);          
        }
        public static int RGB2CMY(int rgb,bool inv) {
          int p0=rgb&255,p1=(rgb>>8)&255,p2=(rgb>>16)&255,r,g,b;
          if(p0<p1) {r=p0;g=p1;} else {r=p1;g=p0;}
          if(p2<r) r=p2;else if(p2>g) g=p2;
          p0=r+g-p0;p1=r+g-p1;p2=r+g-p2;
          r=p0;g=p1;b=p2;
          /*
          if(p0>p1&&p0>p2) {
            if(p1>p2) {r=p2;g=p0-p1+p2;b=p0;}
            else {r=p1;g=p0;b=p0-p2+p1;}
          } else if(p1>p0&&p1>p2) {
            if(p0>p2) {r=p1-p0+p2;g=p2;b=p1;}
            else {r=p1;g=p0;b=p1-p2+p0;}
          } else {
            if(p0>p1) {r=p2-p0+p1;g=p2;b=p1;}
            else {r=p2;g=p2-p1+p0;b=p0;}
          }*/
          if(inv) {r^=255;g^=255;b^=255;}
          return r|(g<<8)|(b<<16);
        }
        public static int RGBShift(int mode,int rgb) {        
          int r=rgb&255,g=(rgb>>8)&255,b=(rgb>>16)&255,x;
          switch(mode) {
           case 3:x=g;g=b;b=x;break;
           case 2:x=r;r=b;b=x;break;
           case 1:x=r;r=g;g=x;break;
           default:x=r;r=g;g=b;b=x;break;
          }
          
          return r|(g<<8)|(b<<16);
        }
        public static int InvertIntensity(int rgb,int mode) { //return RGB2CMY(rgb,true);}
          int r=rgb&255,g=(rgb>>8)&255,b=(rgb>>16)&255;
          int min=r<g?r<b?r:b:g<b?g:b;
          int max=r>g?r>b?r:b:g>b?g:b;
          int del=max-min,sum=r+g+b,sum2=765-sum;
          if(del==0) r=g=b=255-r;
          else if(mode>0) {
            if(mode==1) {
              r=(r-min)+(255-max);
              g=(g-min)+(255-max);
              b=(b-min)+(255-max);
            } else {
              r=(r-min)*255/del;
              g=(g-min)*255/del;
              b=(b-min)*255/del;
              sum=r+g+b;
            }
            sum=r+g+b;
            if(r+g+b>sum2) {
              r=r*sum2/sum;
              g=g*sum2/sum;
              b=b*sum2/sum;              
            } else {
              r=(255-(255-r)*(765-sum2)/(765-sum));
              g=(255-(255-g)*(765-sum2)/(765-sum));
              b=(255-(255-b)*(765-sum2)/(765-sum));
            }
          } else {
            int shf=255-max-min;
            if(shf!=0) {
              r+=shf;
              g+=shf;
              b+=shf;
            }
          }          
          return r|(g<<8)|(b<<16);
        }
        public static int Saturate(int rgb,int w) {
          int r=rgb&255,g=(rgb>>8)&255,b=(rgb>>16)&255;
          int min=r<g?r<b?r:b:g<b?g:b;
          int max=r>g?r>b?r:b:g>b?g:b;
          int del=max-min,sum=r+g+b,sum2=sum;
          if(del==0) return rgb;
          int r2=r,g2=g,b2=b;
          r=(r-min)*255/del;
          g=(g-min)*255/del;
          b=(b-min)*255/del;
          sum=r+g+b;
          if(sum>sum2) {
            r=r*sum2/sum;
            g=g*sum2/sum;
            b=b*sum2/sum;              
          } else {
            r=(255-(255-r)*(765-sum2)/(765-sum));
            g=(255-(255-g)*(765-sum2)/(765-sum));
            b=(255-(255-b)*(765-sum2)/(765-sum));
          }
          if(w>0) {
            int w2=256-w;
            r=(r*w2+w*r2)/256;
            g=(g*w2+w*g2)/256;
            b=(b*w2+w*b2)/256;
          }
          return r|(g<<8)|(b<<16);
        }
        public static int Saturate(int rgb,bool desat,bool over) {
          int r=rgb&255,g=(rgb>>8)&255,b=(rgb>>16)&255;
          int min=r<g?r<b?r:b:g<b?g:b;
          int max=r>g?r>b?r:b:g>b?g:b;
          int del=max-min,sum=r+g+b,sum2=sum;
          if(del==0) return rgb;
          sum/=3;
          if(desat) {
            r=sum+(r-sum)*7/8;
            g=sum+(g-sum)*7/8;
            b=sum+(b-sum)*7/8;
          } else {
            if(del==255) return rgb;
            int del2=del/4,min2=min-del2,max2=max+del2;
            if(over) {
              if(max2>255) max2=255;
              if(min2<0) min2=0;
            } else {
              if(max2>255) {min2+=max2-255;max2=255;}
              if(min2<0) {max2+=min2;min2=0;}
            }
            del2=max2-min2;
            if(del2<=del) return rgb;
            r=min2+(r-min)*del2/del;
            g=min2+(g-min)*del2/del;
            b=min2+(b-min)*del2/del;
          }
          return r|(g<<8)|(b<<16);
        }
        public static int Grayscale(int rgb,int w) {
          int r=rgb&255,g=(rgb>>8)&255,b=(rgb>>16)&255;
          int gr=(r+g+b)/3;
          if(w<=0) r=g=b=gr;
          else {
            int w2=256-w;
            r=(gr*w2+w*r)/256;
            g=(gr*w2+w*g)/256;
            b=(gr*w2+w*b)/256;
          }
          return r|(g<<8)|(b<<16);
        }
        public static bool IsGray(int c) {
          int c0=c&255;
          return (c0|(c0<<8)|(c0<<16))==(c&0xffffff);          
        }
        public static int RGBLevel1(int c,int level) {
          if(level<2||c==255||c==0) return c;
          else if(level==2) return c<128?0:255;
          else if(level==3) return c<85?0:c<170?128:255;
          else {
            int l=c*level/256,l1=255*l/level,l2=255*(l1+1)/level;            
            return 2*c<l1+l2?l1:l2;
          }
        }
        public static int RGBLeveld(int c,int diff) {
          int e,d,c0,c1,c2;
          if(diff<1||diff>128) return c;
          e=diff;d=255-255/e*e;
          c0=c&255;c1=(c>>8)&255;c2=(c>>16)&255;
          c0=c0/e*e;if(c0>127) c0+=d;
          c1=c1/e*e;if(c1>127) c1+=d;
          c2=c2/e*e;if(c2>127) c2+=d;
          return c0|(c1<<8)|(c2<<16);
        }

        public static int RGBSum(int color) {
          return (color&255)+((color>>8)&255)+((color>>16)&255);
        }
        public static int RGBSum2(int color) {
          return ((color&255)+3*((color>>8)&255)+2*((color>>16)&255)+1)/3;
        }
        public static void RGBAdd(int color,ref int s0,ref int s1,ref int s2) {
          s0+=color&255;s1+=(color>>8)&255;s2+=(color>>16)&255;
        }
        public static void RGBAdd(int color,int mul,ref int s0,ref int s1,ref int s2) {
          s0+=mul*(color&255);s1+=mul*((color>>8)&255);s2+=mul*((color>>16)&255);
        }        
        public static void RGBMinMax(int color,ref int min,ref int max,ref int sum) {
          int b=color&255,g=(color>>8)&255,r=(color>>16)&255,i=b,a=b;
          sum+=r+g+b;
          if(g<i) i=g;else if(g>a) a=g;
          if(r<i) i=r;else if(r>a) a=r;
          if(i<min) min=i;if(a>max) max=a;
        }
        public static void RGBMinMax(int color,ref int i0,ref int i1,ref int i2,ref int a0,ref int a1,ref int a2) {
          int b=color&255,g=(color>>8)&255,r=(color>>16)&255,i=b,a=b;
          if(b<i0) i0=b;if(g<i1) i1=g;if(r<i2) i2=r;
          if(b>a0) a0=b;if(g>a1) a1=g;if(r>a2) a2=r;
        }
        public static int RGBMima(int color,int min,int delta) {
           int b=color&255,g=(color>>8)&255,r=(color>>16)&255;
           b=(b-min)*255/delta;
           g=(g-min)*255/delta;
           r=(r-min)*255/delta;
           return b|(g<<8)|(r<<16);
        }
        public static int RGBMima(int color,int min0,int min1,int min2,int dlt0,int dlt1,int dlt2) {
           int b=color&255,g=(color>>8)&255,r=(color>>16)&255;
           b=(b-min0)*255/dlt0;
           g=(g-min1)*255/dlt1;
           r=(r-min2)*255/dlt2;
           return b|(g<<8)|(r<<16);
        }
        public static void RGBMin(bool max,int color,ref int s0,ref int s1,ref int s2) {
          int b=color&255,g=(color>>8)&255,r=(color>>16)&255;
          if(max) {
            if(b>s0) s0=b;if(g>s1) s1=g;if(r>s2) s2=r;  
          } else {
            if(b<s0) s0=b;if(g<s1) s1=g;if(r<s2) s2=r;
          }
        }
        public static void RGBMin(int color,ref int s0,ref int s1,ref int s2) { RGBMin(false,color,ref s0,ref s1,ref s2);}
        public static void RGBMax(int color,ref int s0,ref int s1,ref int s2) { RGBMin(true,color,ref s0,ref s1,ref s2);}
        public static void RGBIMin(bool max,int color,ref int s) {
          int c=RGBSum(color),d=RGBSum(s);
          if(max?c>d:c<d) s=color;
        }
        public static int RGBCmp(int color) {
          int b=(color&255),g=(color>>8)&255,r=(color>>16)&255;
          return ((r+g+b)<<16)|(g<<8)|r;
        }
        public static int RGBCmp2(int color) {
          int b=(color&255),g=(color>>8)&255,r=(color>>16)&255;
          return ((2*r+3*g+b)<<16)|(g<<8)|r;
        }
        public static int RGBSatur(int color) {
          int b=color&255,g=(color>>8)&255,r=(color>>16)&255,i=b,a=b;
          if(g<i) i=g;else if(g>a) a=g;
          if(r<i) i=r;else if(r>a) a=r;
          if(i==0&&a==255) return color;
          if(i==a) return 0;
          a-=i;
          b=(b-i)*255/a;g=(g-i)*255/a;r=(r-i)*255/a;
          return b|(g<<8)|(r<<16);
        }
        public static int RGBHue(int color,bool rev,int shift) {
          int b=color&255,g=(color>>8)&255,r=(color>>16)&255,i=b,a=b,d,h;
          if(r<g) {i=r;a=g;} else {i=g;a=r;};
          if(b<i) i=b;else if(b>a) a=b;
          if(i==a) return rev||shift!=0?color:-1;
          d=a-=i;
          if(d!=255) {b=(b-i)*255/d;g=(g-i)*255/d;r=(r-i)*255/d;}
          if(g==255) h=512+(r>0?-r:b);
          else if(b==255) h=1024+(g>0?-g:r);
          else h=b>0?1536-b:g;
          if(shift==0&&!rev) return h;
          if(rev) h=1536-h;
          h+=shift;
          color=HueRGB(h);
          if(d==255) return color;
          b=color&255;g=(color>>8)&255;r=(color>>16)&255;
          r=i+r*d/255;g=i+g*d/255;b=i+b*d/255;
          return b|(g<<8)|(r<<16);
        }
        public static int HueRGB(int h) {
          if(h>=1536) h=h%1536;
          else if(h<0) h=h%1536+1536;
          int d=h&255,r,g,b;
          h>>=8;
          if(h==0) {r=255;g=d;b=0;}
          else if(h==1) {r=255^d;g=255;b=0;}
          else if(h==2) {r=0;g=255;b=d;}
          else if(h==3) {r=0;g=255^d;b=255;}
          else if(h==4) {r=d;g=0;b=255;}
          else {r=255;g=0;b=255^d;}
          return b|(g<<8)|(r<<16);
        }
        public static int RGBSub(int color,int scolor,int mode,bool sharp) {
          //if(mode==0) return RGBReplace(color,scolor,0,0);
          //if(mode==1) return RGBReplace(color,scolor,0xffffff,0);
          //if(mode==2) return RGBReplace(color,scolor,RGBGray(RGBSum(scolor)/3),0);
          int c0=color&255,c1=(color>>8)&255,c2=(color>>16)&255;
          int s0=scolor&255,s1=(scolor>>8)&255,s2=(scolor>>16)&255;
          int mi,ma;
          if(c0<c1) {mi=c0;ma=c1;} else {mi=c1;ma=c0;}
          if(c2<mi) mi=c2;else if(c2>ma) ma=c2;
          if(mi==ma) return color;
          int d=1,e=1,x;
          c0-=mi;c1-=mi;c2-=mi;
          if(mode==1) { x=ma-mi;c0=x-c0;c1=x-c1;c2=x-c2;s0^=255;s1^=255;s2^=255;} 
          if(s0>0&&e*(c0)<s0*d) {d=c0;e=s0;}
          if(s1>0&&e*(c1)<s1*d) {d=c1;e=s1;}
          if(s2>0&&e*(c2)<s2*d) {d=c2;e=s2;}
          if(d==0||e==0) return color;
          s0=s0*d/e;s1=s1*d/e;s2=s2*d/e;
          if(sharp) {
            int f=abs(s0,c0)+abs(s1,c1)+abs(s2,c2),s=s0+s1+s2;
            if(f>s) return color;
            if(f>0) {s0=s0*(s-f)/s;s1=s1*(s-f)/s;s2=s2*(s-f)/s;}
          }
          if(mode>2) {
            c0+=mi;c1+=mi;c2+=mi;
            return Palette.ColorIntensity765(c0,c1,c2,c0+c1+c2-s0-s1-s2,mode>3?1:0);
          }
          if(mode==1) { x=ma-mi;c0=x-c0+s0;c1=x-c1+s1;c2=x-c2+s2;} 
          else {c0-=s0;c1-=s1;c2-=s2;}
/*          if(mode==1) {
            int sma=s0>s1?s0:s1;
            if(s2>sma) sma=s2;
            s0=sma-s0;s1=sma-s1;s2=sma-s2;
            c0+=s0;
            c1+=s1;
            c2+=s2;
            int cma=c0,l=255-mi;
            if(c1>cma) cma=c1;
            if(c2>cma) cma=c2;
            if(cma>l) { c0=c0*l/cma;c1=c1*l/cma;c2=c2*l/cma;}              
          } else {
            c0-=s0;c1-=s1;c2-=s2;            
          }*/
          c0+=mi;c1+=mi;c2+=mi;
          return mode==2?Palette.ColorIntensity765(c0,c1,c2,c0+c1+c2+s0+s1+s2,1):(c0|(c1<<8)|(c2<<16));
        }

        public static int RGBReplace(int color,int scolor,int rcolor,int absolute) {
          int c0=color&255,c1=(color>>8)&255,c2=(color>>16)&255;
          int s0=scolor&255,s1=(scolor>>8)&255,s2=(scolor>>16)&255;
          int mi,ma;
          if(absolute>0) {
            int e=abs(c0,s0)+abs(c1,s1)+abs(c2,s2);
            return e<absolute?Palette.RGBMix(rcolor,color,e,absolute):color;
          } else {
            if(c0<c1) {mi=c0;ma=c1;} else {mi=c1;ma=c0;}
            if(c2<mi) mi=c2;else if(c2>ma) ma=c2;
            if(mi==ma) return color;
            c0-=mi;c1-=mi;c2-=mi;
            s0=s0*(ma-mi)/255;s1=s1*(ma-mi)/255;s2=s2*(ma-mi)/255;
            int d=ma-mi,e=abs(s0,c0)+abs(s1,c1)+abs(s2,c2);
            if(e>=d) return color;
            s0=s0*(d-e)/d;s1=s1*(d-e)/d;s2=s2*(d-e)/d;
            int r0=rcolor&255,r1=(rcolor>>8)&255,r2=(rcolor>>16)&255;
            r0=r0*(d-e)/255;r1=r1*(d-e)/255;r2=r2*(d-e)/255;
            c0+=mi+r0-s0;c1+=mi+r1-s1;c2+=mi+r2-s2;
          }
          if(c0<c1) {mi=c0;ma=c1;} else {mi=c1;ma=c0;}
          if(c2<mi) mi=c2;else if(c2>ma) ma=c2;          
          if(mi<0||ma>255) {
            int mi2=mi<0?0:mi,ma2=ma>255?255:ma;
            c0=mi2+(c0-mi)*(ma2-mi2)/(ma-mi);
            c1=mi2+(c1-mi)*(ma2-mi2)/(ma-mi);
            c2=mi2+(c2-mi)*(ma2-mi2)/(ma-mi);
          }
          return c0|(c1<<8)|(c2<<16);
        }
        public static int RGBGray(int gray) {
          return 0x10101*(gray&255);
        }
        public static int Levels(int rgb,int levels) {
          int s=RGBSum(rgb);
          s=s*levels/(765+levels-1);
          return ColorIntensity765(rgb,s*765/levels);
        }
        public static int Strips(int rgb,int levels,int c0,int c1) {
          int s=RGBSum(rgb);
          s=s*levels/(765+levels-1);
          return 0!=(s&1)?c1:c0;
        }
        public static int ToWhite(int color,int mul) {
          int b=color&255,g=(color>>8)&255,r=(color>>16)&255;
          int mul1=mul+1;
          b=b*mul1/mul;
          g=g*mul1/mul;
          r=r*mul1/mul;
          return RGBAvg2(1,b,g,r);
        }
        public static int ToBlack(int color,int mul) {
          int b=color&255,g=(color>>8)&255,r=(color>>16)&255;
          int mul1=mul+1;
          b=255-(255-b)*mul1/mul;
          g=255-(255-g)*mul1/mul;
          r=255-(255-r)*mul1/mul;
          return RGBAvg2(1,b,g,r);
        }
        public static Color IntColor(int rgb) {
          return System.Drawing.Color.FromArgb((255<<24)|rgb);
        }
      public static int TextColor(int color) {
        byte r=(byte)color,g=(byte)(color>>8),b=(byte)(color>>16);
			  if(g>0x76||r>0x87&&b>0x87) return 0;
        byte m=r>g?r>b?r:b:g>b?g:b;
        int s=299*r+587*g+114*b;
        return s<=160000?(int)0xffffff:0;
      }
      public static int[] Render(int[] pal,int len) {
        int[] res=new int[len--];
        for(int i=0;i<=len;i++)
          res[i]=RGBMix(pal,i,len);
        return res;
      }
      public static int[] Render(int color,int len) {
        int[] res=new int[len--];
        //color=RGBSatur(color);
        int cen=RGBSum(color);
        cen=cen*len/765;
        for(int i=0;i<=len;i++)
          res[i]=i<cen?RGBMix(0,color,i,cen):RGBMix(color,0xffffff,i-cen,len-cen);
        return res;
      }
      public static byte[] Saw256(int mode) {
        byte[] r=new byte[256];
        for(int i=0;i<256;i++) {
          int o;
          switch(mode) {
           case 7:o=i^255;break;
           case 6:o=i-32;break;
           case 5:o=i+32;break;
           case 3:o=i<128?255-2*i:2*i-255;break;
           case 2:o=i<128?2*i:2*255-2*i;break;
           case 1:o=(2*i+((i&128)!=0?1:0));break;
           case 4:
           default:
            o=(i<=85?3*i:i<=170?255-3*(i-85):3*(i-170));
            if(mode==4) o^=255;
            break;
          }
          r[i]=(byte)o;
        }
        return r;
      }
      public static byte[] n256(int n) {
        byte[] r=new byte[256];
        int d=n<3?128:255/(n-1),a=d/2;
        for(int i=0;i<256;i++) 
          r[i]=(byte)(n<3?i>127?255:0:((i*2*(n-1))/255+1)/2*255/(n-1));
        return r;
      }
      static int hex(char ch) {
        return ch>='0'&&ch<='9'?ch-'0':ch>='a'&&ch<='f'?ch-'a'+10:ch>='A'&&ch<='F'?ch-'A'+10:0;
      }
      public static int Parse(string color) {
        int c;
        if(""+color=="") return -1;
        color=color.Trim();
        if(int.TryParse(color,out c)) return c&0xffffff;
        if(color[0]!='#') return -1;
        c=0;
        for(int i=1;i<color.Length;i++)
          c=(c*16)|hex(color[i]);
        if(color.Length==2) c*=0x111111;
        else if(color.Length==3) c*=0x10101;
        else if(color.Length==4) c=0x11*((c&15)|((c<<4)&0xf00)|((c<<8)&0xf0000));
        return c;
      }
      public static int[] Parse(string[] color) {
        int[] res=new int[color.Length];
        for(int i=0;i<res.Length;i++)
          res[i]=Parse(color[i]);
        return res;
      }
      public static int Colorize(int cmode,int c,int p) {
        int p0=p&255,p1=(p>>8)&255,p2=(p>>16)&255,pmi,pma,pi;
        int c0=c&255,c1=(c>>8)&255,c2=(c>>16)&255,cmi,cma,ci;
        if(c0<c1) {cmi=c0;cma=c1;} else {cmi=c1;cma=c0;}
        if(c2<cmi) cmi=c2;else if(c2>cma) cma=c2;
        if(p0<p1) {pmi=p0;pma=p1;} else {pmi=p1;pma=p0;}
        if(p2<pmi) pmi=p2;else if(p2>pma) pma=p2;
        ci=(7471*c0+38470*c1+19595*c2)>>16;
        pi=(7471*p0+38470*p1+19595*p2)>>16;
        ci=(c0+c1+c2)/3;
        pi=(p0+p1+p2)/3;
        switch(cmode) {
          case 6:
          p0=(c0+3*p0)/4;
          p1=(c1+3*p1)/4;
          p2=(c2+3*p2)/4;
          break;
          case 5:
          p0=255-(c0^255)*(255^p0)/255;
          p1=255-(c1^255)*(255^p1)/255;
          p2=255-(c2^255)*(255^p2)/255;
          break;
          default:
          case 4:
          if(pmi<pma&&cmi<cma) {
            p0=pi+(c0-ci)*(pma-pmi)/(cma-cmi);
            p1=pi+(c1-ci)*(pma-pmi)/(cma-cmi);
            p2=pi+(c2-ci)*(pma-pmi)/(cma-cmi);
            if(p0<0) p0=0;else if(p0>255) p0=255; 
            if(p1<0) p1=0;else if(p1>255) p1=255; 
            if(p2<0) p2=0;else if(p2>255) p2=255; 
          }             
          break;
          case 3:
          if(pi<ci) {
            p0=c0*pi/ci;
            p1=c1*pi/ci;
            p2=c2*pi/ci;
          } else if(pi>ci) {
            p0=255-(255-c0)*(255-pi)/(255-ci);
            p1=255-(255-c1)*(255-pi)/(255-ci);
            p2=255-(255-c2)*(255-pi)/(255-ci);
          } else {
            p0=c0;p1=c1;p2=c2;
          }
          break;
          case 2:
          p0=p0>=c0?255:255*p0/c0;
          p1=p1>=c1?255:255*p1/c1;
          p2=p2>=c2?255:255*p2/c2;
          if(p1<p0) p0=p1;
          if(p2<p0) p0=p2;
          if(pi>0) {
            if(pi<p0) pi=p0;
            p0=pi*c0/255;
            p1=pi*c1/255;
            p2=pi*c2/255;
          } else {
            p0=p&255;p1=(p>>8)&255;p2=(p>>16)&255;
          }                
          break;
          case 1:
          p0-=pmi*(255-c0)/255;
          p1-=pmi*(255-c1)/255;
          p2-=pmi*(255-c2)/255;
          break;
        }
        return p0|(p1<<8)|(p2<<16);
      }
    }
}