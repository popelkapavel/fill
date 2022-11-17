using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace fill {
	public partial class fBoard : Form {
	  fMain Main;
		internal bool Updates;
		internal List<string> Brushes=new List<string>();
		internal List<string> Shapes=new List<string>();

		void Brush(string name,string code) {
		  Brushes.Add(name);Brushes.Add(code);
		}
		void Shape(string name,string code) {
		  Shapes.Add(name);Shapes.Add(code);
		}
    public bool TextInput {get{
      return Visible&&(tbText.Focused||FontFamily.Focused||tbTLRound.Focused||tbDash.Focused);
    }}
    public int FillMix{get{return 0;}}
    public fBoard(fMain main) {
			InitializeComponent();
      bwpatt.SelectedItem=bwpatt.Items[0];
      tNewWidth.Text=""+Screen.PrimaryScreen.Bounds.Width;
      tNewHeight.Text=""+Screen.PrimaryScreen.Bounds.Height;
			Main=main;
			cbRepeatMode.SelectedIndex=0;
			cbFill.SelectedIndex=cbSquare.SelectedIndex=0;
      SetGammax(0);
			
			Brush("Dot",null);
			Brush("Box 2x2","11.11");
			Brush("Plus 3x3","010.111.010");
			Brush("Circle 4.4","0110.1111.1111.0110,1111.1111.1111.1111");
			Brush("Circle 5x5","01110.11111.11111.11111.01110,00100.01110.11111.01110.00100");
			Brush("Vertical |","11.11.11.11.11.11.11.11");
			Brush("Horizontal --","11111111.11111111");
			Brush("Double dot :","111.111.111.000.000.000.111.111.111");
			Brush("Raise /","00000011.00000111.00001110.00011100.00111000.01110000.11100000.11000000");
			Brush("Fall \\","11000000.11100000.01110000.00111000.00011100.00001110.00000111.00000011");
			Brush("Diamond 7x7","0001000.0011100.0111110.1111111.0111110.0011100.0001000");
			Brush("Octa 10x10","0001111000.0011111100.0111111110.1111111111.1111111111.1111111111.1111111111.0111111110.0011111100.0001111000");
			cbBrush.Items.Clear();
			for(int i=0;i<Brushes.Count;i+=2)
			  cbBrush.Items.Add(Brushes[i]);
			cbBrush.SelectedIndex=0;

			Shape("Rectangle","rectangle,rounded");
			Shape("Diamond","diamond,diamond2");
			Shape("Triangle","triangle,triangle90");
			Shape("Arrow","arrow,arrow2");
			Shape("Hexagon","hexagon,hexagon2,hexagon2,star6");
			Shape("Octa","octa,octa2");
			Shape("Circle","circle,circle2");
      Shape("Valve","valve");
			Shape("Star 8/16","star8,star16");
			Shape("Star 5","star,penta");
			cbShape.Items.Clear();
			for(int i=0;i<Shapes.Count;i+=2)
			  cbShape.Items.Add(Shapes[i]);
			cbShape.SelectedIndex=0;

			Updates=true;
		}

    public RepeatOp RepeatMode {get { return (RepeatOp)(1+cbRepeatMode.SelectedIndex);}}
    public int Fill4() {
      int fi=cbFill.SelectedIndex;
      return fi==6?4:fi==5?8:fi==4?2:1;
    }
    static int[] sqa=new int[] {0,1,3,4,5,6,7,8,9,10,11,12,16,17,19,20,21,22,23,32,33,34,48,49,50,51,52,53,54,64,65,80,81,83,84,85,86,87,92};
    public int FillSquare() {
      int fi=sqa[cbSquare.SelectedIndex];
      return fi;
    }
		protected override void OnClosing(CancelEventArgs e) {
		  e.Cancel=true;
			Hide();
		}

		private void Update(object sender, EventArgs e) {
		  if(sender is Button) { Cmd(sender,e);return;}
		  if(Updates) Main.UpdateBoard();
		}
    const int FontMaxSize=16;
    internal Font GetFont(int maxsize) {
      int size;
      if(!int.TryParse(FontSize.Text,out size)||size<6) size=10;
      return new Font(FontFamily.Text,maxsize<6||size<maxsize?size:maxsize,(FontBold.Checked?FontStyle.Bold:0)|(FontItalic.Checked?FontStyle.Italic:0)|(FontUnder.Checked?FontStyle.Underline:0)|(FontStrike.Checked?FontStyle.Strikeout:0));
    }
    static string cs(bool c,bool s,string sn,string sc,string ss,string scs) {
      return c?s?scs:sc:s?ss:sn;
    }
    static int Bc(Button b) {
      return b.BackColor.ToArgb()&0xffffff;
    }
    public int FillLimit{ get { return (int)fillLimit.Value;}}
    public int AvgShape { get { return rAV1.Checked?0:rAV2.Checked?1:rAV3.Checked?2:-1;}}
		private void Cmd(object sender, EventArgs e) {
		  Control btn=sender as Control;
		  string cmd=""+btn.Tag;
      if(cmd=="text") Close();      
      if(cmd.StartsWith("chan ")&&chChanColored.Checked) 
       cmd="chan "+(16|int.Parse(cmd.Substring(5)));
      if(cmd.StartsWith("gro ")&&checkGrx.Checked) cmd="grx"+cmd.Substring(3);
      int bl;
			switch(cmd) {
       case "font dialog":
        FontDialog fd=new FontDialog();
        fd.Font=GetFont(0);
        if(DialogResult.OK==fd.ShowDialog(this)) {
          Font fn=fd.Font;
          FontFamily.Text=fn.Name;
          FontSize.Text=""+(int)fn.Size;
          FontBold.Checked=fn.Bold;
          FontItalic.Checked=fn.Italic;
          FontUnder.Checked=fn.Underline;
          FontStrike.Checked=fn.Strikeout;
          tbText.Font=fn.Size<16?fn:GetFont(16);
        }
        break;        
       case "fillpat on":Main.SetFillPattern(false,false);return;
       case "fillpat off":Main.SetFillPattern(false,true);return;
       case "edge":Main.ProcessCommand(GDI.ShiftKey||GDI.CtrlKey?"edge1":"edge");break;
       case "emboss":Main.ProcessCommand(cmd+cs(GDI.CtrlKey,GDI.ShiftKey," 4"," 8"," 16"," 32"));break;
       case "gray":Main.ProcessCommand(cmd+(BWMin.Checked?" min":BWMax.Checked?" max":BWSQR.Checked?" sqr":BWEYE.Checked?" eye":""));break;
       case "satur 2":Main.ProcessCommand(cmd+(chSaturOver.Checked?"over":""));break;
       case "gamma -":
       case "gamma +":{ int g=X.t(textBox1.Text,25);Main.ProcessCommand("gamma "+(Palette.range(100-g,1,99)/100.0)+(cmd[6]=='-'?" inv":""));} break;
			 case "levels":Main.ProcessCommand("levels "+levelsCount.Text);break;
			 case "strips":Main.ProcessCommand("strips "+levelsCount.Text);break;
			 case "bw":Main.ProcessCommand("bw "+BWLevel.Text+(BWSQR.Checked?" sqr":BWMax.Checked?" max":BWMin.Checked?" min":BWEYE.Checked?" eye":""));break;
			 case "c8":Main.ProcessCommand("c8 "+BWLevel.Text);break;
       case "remove_dots":Main.ProcessCommand("remove_dots "+(chRemDotWhite.Checked?" wh":"")+(chRemDotBlack.Checked?" bl":"")+(rRemDot3.Checked?" 3":rRemDot8.Checked?" 8":" 4"));break;
       case "remove_dust":Main.ProcessCommand("remove_dust "+dustLevel.Text+" "+(blackDust.Checked?" bl":"")+(whiteDust.Checked?" wh":""));break;
       case "neq2":Main.ProcessCommand("neq2 "+borderLevel.Text+(GDI.CtrlKey?" 0":" 4")+" 8"+(X8.Checked?" x8":""));break;       
       case "hdr4":Main.ProcessCommand("hdr4"+(chSaturOver.Checked?" satur":"")+(cbContrast.Checked?" rgb":"")+(X8.Checked?"":" x8"));break;
       case "hdr":
       case "neq":Main.ProcessCommand(cmd+(cbInvertBW.Checked?" bw":"")+(X8.Checked?" x8":""));break;
       case "bold":
       case "bold vert":Main.ProcessCommand(cmd+(chBoldMax.Checked?" max":""));break;
       case "sharp":
       case "blur":Main.ProcessCommand(cmd+(chBrightBW.Checked?" bw":"")+(X8.Checked?" x8":""));break;
       case "avg 4 1":Main.ProcessCommand("avg 4"+(GDI.CtrlKey?" 9":" 1")+(GDI.ShiftKey?" -4":" +4")+" "+AvgShape);break;
       case "avg 4 2":Main.ProcessCommand("avg 4"+(GDI.CtrlKey?" 10":" 2")+(GDI.ShiftKey?" -4":" +4")+" "+AvgShape);break;
       case "avg 4 4":Main.ProcessCommand("avg 4"+(GDI.CtrlKey?" 12":" 4")+(GDI.ShiftKey?" -6":" +6")+" "+AvgShape);break;
       case "c765":Main.ProcessCommand(cmd+(cbContrast.Checked?" rgb":(GDI.ShiftKey?"":GDI.CtrlKey?" satur2":" satur")));break;
       case "maxcount":{ int n;int.TryParse(cbMaxCount.Text,out n);if(n<2) n=256;else if(n>256) n=256;Main.ProcessCommand(cmd+" "+n+(chMaxCountMax.Checked?"max":""));break;}
			 case "border":
        if(GDI.CtrlKey) {
          int x;
          if(int.TryParse(borderLevel.Text,out x)) borderLevel.Text=""+(x+1);
          Main.Undo(false);
        }
        Main.ProcessCommand("border "+borderLevel.Text+(X8.Checked?" x8":""));
        if(GDI.ShiftKey) { 
          int x;
          if(int.TryParse(borderLevel.Text,out x)&&x>0) borderLevel.Text=""+(x-1);
        }
        break;
			 case "invert":Main.ProcessCommand("invert "+(cbInvertIntensity.Checked?" intensity":"")+(cbInvertBW.Checked?" bw":""));break;
			 case "contrast":Main.ProcessCommand("contrast "+(cbContrast.Checked?" rgb":""));break;
       case "dark":
       case "bright":Main.ProcessCommand(cmd+(chBrightBW.Checked?" bw":""));break;
			 case "expand":
			  Main.ProcessCommand("expand"+(X8.Checked?" x8":"")+(ExpandWOnly.Checked?" wonly":"")+(ExpandWhite.Checked?" white":""));
				break;
			 case "impand":
			  Main.ProcessCommand("impand "+(chImpandBlack.Checked?0:Main.Color1)+" "+Main.Color2+(ExpandWhite.Checked?" repeat":""));
				break;
       case "rgb":if(GDI.ShiftKey|GDI.CtrlKey) cmd+=" back";goto default;
       case "rgb cmy":if(GDI.ShiftKey|GDI.CtrlKey) cmd+=" inv";goto default;
       case "remove":cmd+=(chImpandBlack.Checked?" bl":"")+(chRemoveRepeat.Checked?" repeat":"");goto default;
       case "erasec":cmd+=rEraseH.Checked?" h":rEraseV.Checked?" v":" h v";goto default;
       case "outline":if(chImpandBlack.Checked) cmd+=" bl";goto default;
       case "replace":if(GDI.CtrlKey) cmd+=" bl2";if(GDI.ShiftKey) cmd+=" wh2";goto default;
       case "saveicon":cmd="save icon";if(chIconCursor.Checked) cmd+=" cursor";if(GDI.ShiftKey) cmd+=" as";goto default;
       case "paln":cmd+=" "+palN.Value+(palNAvg.Checked?" avg":"");goto default;
       case "paper":cmd+=" "+papN.Value+(chPaperB.Checked?" black":"")+(chPaperT.Checked?" transp":"")+(chPaperX.Checked?" xy":"");goto default;
       case "tess":cmd+=" "+ (6+2*papW.Value)+" "+papN.Value+(chPaperX.Checked?" mima3":"");goto default;
       case "diffuse":
       case "matrix":cmd+=" "+(palN.Value-1) +(chMatrixRGB.Checked?" rgb":"");goto default;
       case "subf":cmd="sub "+(Palette.RGBSatur(Main.Color1)+(chSharpSub.Checked?" s":""));goto default;
       case "subfi":case "subfw":
       case "subfg":cmd="sub "+(Palette.RGBSatur(Main.Color1)+" "+cmd[4]+(chSharpSub.Checked?" s":""));goto default;
       case "replacec @f @b":cmd+=(chReplAbsolute.Checked?" a":"");goto default;
       case "replacediff":int lvl;cmd+=" x x "+GetFillDiff(out lvl)+" "+lvl;goto default;       
       case "average":cmd+=(chAvgHori.Checked?" h":"")+(chAvgVert.Checked?" v":"");goto default;
       case "pattern":cmd+=" "+bwpatt.Text +" "+bwpatSize.Value+" "+bwpatWidth.Value+(bwpattInv.Checked?" inv":"");goto default;
       case "contour":cmd+=GDI.CtrlKey?GDI.ShiftKey?" stroke inv":" fill":GDI.ShiftKey?" fill inv":" stroke";goto default;
       case "comp":cmd+=(chCompHori.Checked||!chCompVert.Checked?" hori":"")+(chCompVert.Checked?" vert":"");goto default;
       case "rollv":cmd="roll 0 "+(GDI.ShiftKey?"-":"")+RollSize.Text;goto default;
       case "rollh":cmd="roll "+(GDI.ShiftKey?"-":"")+RollSize.Text;goto default;
       case "half":cmd="half "+halfRat.SelectedIndex+(halfBack.Checked?" back":halfWhite.Checked?" white":halfBlack.Checked?" black":halfMin.Checked?" min":halfMax.Checked?" max":"")+(chAvgHori.Checked?" hori":"")+(chAvgVert.Checked?" vert":"");goto default;
       case "c4":cmd+=" "+Bc(bc00)+" "+Bc(bc01)+" "+Bc(bc10)+" "+Bc(bc11);//+(chC5.Checked?" "+Bc(bcc):"");
        goto default;
			 default:
        if(cmd.StartsWith("pal1 ")||cmd.StartsWith("pal ")) {
          if(btn.Parent.Name=="tp8")
            cmd+=" alpha="+(((int)EfAlpha.Value)*256/10);
        }
        if(cmd.Contains("{")) {
        }
        Main.ProcessCommand(cmd);
        break;
			}      
		}
    static Control Focusing(Control x) {
      IContainerControl c=x as IContainerControl;
      while(c!=null) {
        x=c.ActiveControl;
        c=x as IContainerControl;
      }
      return x;
    }

    bool ToUpper(TextBox tb) { 
      if(tb==null) return false;
      if(tb.SelectionLength<1) tb.SelectAll();
      int b=tb.SelectionStart;
      string s=tb.SelectedText;
      bool up=false,lo=false;
      foreach(char ch in s)
        if(char.IsUpper(ch)) up=true;else if(char.IsLower(ch)) lo=true;
      if(up) {
        s=lo?s.ToUpperInvariant():s.ToLowerInvariant();
      } else {
        char[] cha=new char[s.Length];
        bool lt=false;
        for(int i=0;i<s.Length;i++) {
          char ch=s[i];
          cha[i]=lt?char.ToLower(ch):char.ToUpper(ch);
          lt=char.IsLetterOrDigit(ch);
        }
        s=new string(cha);
      }
        
      tb.Text=tb.Text.Substring(0,b)+s+tb.Text.Substring(b+s.Length);
      tb.SelectionStart=b;
      tb.SelectionLength=s.Length;
      return true;
    }

    void SetTab(int tab) {
      if(tab==tabControl1.SelectedIndex) Hide();
      tabControl1.SelectTab(tab);
    }
    protected override bool ProcessCmdKey(ref Message msg,Keys keyData) {
      TextBox ftb;
      switch(keyData) {
  		 case Keys.F1:Main.ProcessCommand("help");return true;
			 case Keys.Escape:
			 case Keys.F10:Close();return false;
			 case Keys.F11:Main.Fullscreen();return true;
       case Keys.U|Keys.Control:
        return ToUpper(Focusing(this) as TextBox);
       case Keys.A|Keys.Control:
         ftb=Focusing(this) as TextBox;
         if(ftb!=null) ftb.SelectAll();
         else Main.SelectAll();
         return true;
       case Keys.Oem3|Keys.Alt:
       case Keys.Oem3|Keys.Control:SetTab(0);return true;
       case Keys.D1|Keys.Alt:
       case Keys.D1|Keys.Control:SetTab(1);return true;
       case Keys.D2|Keys.Alt:
       case Keys.D2|Keys.Control:SetTab(2);return true;
       case Keys.D3|Keys.Alt:
       case Keys.D3|Keys.Control:SetTab(3);return true;
       case Keys.D4|Keys.Alt:
       case Keys.D4|Keys.Control:SetTab(4);return true;
       case Keys.D5|Keys.Alt:
       case Keys.D5|Keys.Control:SetTab(5);return true;
       case Keys.D6|Keys.Alt:
       case Keys.D6|Keys.Control:SetTab(6);return true;
       case Keys.D7|Keys.Alt:
       case Keys.D7|Keys.Control:SetTab(7);return true;
       case Keys.D8|Keys.Alt:
       case Keys.D8|Keys.Control:SetTab(8);return true;
       case Keys.D9|Keys.Alt:
       case Keys.D9|Keys.Control:SetTab(9);return true;
       case Keys.Z|Keys.Control:Main.Undo();break;
       case Keys.Y|Keys.Control:Main.Redo();break;
			}
			return false;
		}

		private void cbBrush_SelectedIndexChanged(object sender, EventArgs e) {
		  if(!Updates) return;  
		  int i=2*cbBrush.SelectedIndex+1;
			if(Brushes.Count>i) Main.ProcessCommand("brush "+Brushes[i]);
		   
		}

		private void cbShape_SelectedIndexChanged(object sender, EventArgs e) {
		  if(!Updates) return;  
		  int i=2*cbShape.SelectedIndex+1;
			if(Brushes.Count<=i) return;
			string shape=Shapes[i];
			if(shape.IndexOf(',')>=0) {
        string[] sa=shape.Split(',');
        i=0;
        if(GDI.ShiftKey) i=2;
        if(GDI.CtrlKey) i++;
        shape=sa[i<sa.Length?i:sa.Length-1];
			}
			Main.ProcessCommand("shape "+shape);
		}

    private void pasteDiff_CheckedChanged(object sender, EventArgs e) {
      if(Main.MovePaste) {
        Main.MoveXor=GetDiff()>0;
        Main.UpdatePaste();
      }
    }

    private void SnapChanged(object sender, EventArgs e) {
      string txt=Snap.Text;      
      int snap=0;
      if(chSnap.Checked&&txt!=""&&!int.TryParse(txt,out snap)) Snap.Text="0";      
      Main.snap=snap>1?snap:0;

    }

    private void UpdateFont(object sender, EventArgs e) {
      tbText.Font=GetFont(FontMaxSize);
    }

    private void UpdateFont(object sender, UICuesEventArgs e)
    { tbText.Font=GetFont(FontMaxSize);

    }

    private void btColor_Click(object sender, EventArgs e)
    { 
      Button b=sender as Button;
      if(GDI.CtrlKey) b.BackColor=b.BackColor==Color.Black?Color.White:Color.Black;
      else if(GDI.ShiftKey) {
        Color c1=Palette.IntColor(Main.Color1);
        b.BackColor=b.BackColor==c1?Palette.IntColor(Main.Color2):c1;
      }
      else Main.ChooseColor(b);
    }

    private void Color_Click(object sender, EventArgs e) { Color_Click(sender,e,MouseButtons.Left);}
    private void Color_MouseUp(object sender, MouseEventArgs e) { Color_Click(sender,e,e.Button);}
    private void Color_Click(object sender, EventArgs e,MouseButtons mb) { 
      Button b=sender as Button;
      switch(b.Name) {
       case "bModeFill":case "bModeFloat":
       case "bModeBorder":
       case "bModeLinear":
       case "bModeRadial":case "bModeSquare":
       case "bModeSelect":
       case "bModeCircle":Main.bGradMode_Click(sender,mb);break;
       case "bDrawMorph":
       case "bDrawEdge":
       case "bDrawPolar":
       case "bDrawRect":
       case "bDrawLine":
       case "bDrawReplace":
       case "bDrawFree":Main.miDraw_Click(sender,mb);break;
       case "bClear":Main.bClear_Click(sender,e);break;
       case "bColor1":case "bColor2":Main.bColor_Click2(sender,e);break;
       case "bSwap":Main.bSwap_Click(sender,e);break;
       case "bGamma0":
       case "bGamma1":
       case "bGamma2":
       case "bGamma3":
       case "bGamma4":SetGammax(int.Parse(b.Tag+""));break;

//       default:Main.bColor_Click(sender,e);break;
      }
    }

    private void Color_MouseDown(object sender, MouseEventArgs e) {
      if(sender==bColor1||sender==bColor2)
        Main.bColor2_MouseDown(sender,e);
      else 
        Main.bColor_MouseDown(sender,e);
    }

    private void ltlWidth_Click(object sender, EventArgs e) { LabelClick(false,tbTLWidth2);}    
    private void ltlWidth_DoubleClick(object sender, EventArgs e) { LabelClick(false,tbTLWidth2);}
    private void LabelMouseDown(object sender, MouseEventArgs e) {
      LabelClick(e.Button==MouseButtons.Right^(GDI.CtrlKey),sender==lPadding?tPadding:sender==ltlWidth?tbTLWidth2:tbTlDash2);
    }

    void LabelClick(bool dec,NumericUpDown nud) {
      decimal nv=nud.Value+(dec?-1:1);
      if(nv>=nud.Minimum&&nv<=nud.Maximum)
        nud.Value=nv;
      
    }
    void IntTextClick(TextBox tb,bool dec,int min,int max) {
      int s;
      if(int.TryParse(tb.Text,out s)) {
        s+=dec?-1:1;
        if(s>=min&&s<=max) tb.Text=""+s;

      }
    }

    private void lIntText_MouseDown(object sender, MouseEventArgs e) {
      if(sender==lFontSize)      
        IntTextClick(FontSize,e.Button==MouseButtons.Right^(GDI.CtrlKey),5,int.MaxValue);
      else if(sender==lMixLevel)      
        IntTextClick(MixLevel,e.Button==MouseButtons.Right^(GDI.CtrlKey),0,100);
    }

    private void tbDash_TextChanged(object sender, EventArgs e) {
      Main.UpdateDash();

    }
    public int GetMix() {
      if(!pasteMix.Checked) return 0;
      int m;
      return int.TryParse(MixLevel.Text,out m)&&m>0?m:0;
      
    }
    public void BOn(bool on,Button x) {      
      x.ForeColor=on?Color.White:Color.Black;
      x.BackColor=on?Color.Black:SystemColors.Control;
    }
    public void Bon(int on,params Button[] ba) {
      foreach(Button b in ba) BOn(on--==0,b);
    }
    int Gx=0;
    public int GetGammax() {return (Gx&15)|((int)(nGammaSteps.Value-1)<<8)|((int)(nGammaDents.Value-1)<<4);}
    public int SetGammax(int gx) {
      int g2=gx;
      Gx=gx;
      Bon(gx,bGamma0,bGamma1,bGamma2,bGamma3);
      return g2;
    }

    public int GetPasteFilter() {
      if(rbFilterOff.Checked) return -1;
      if(rbFilterWhite.Checked) return bmap.White;
      if(rbFilterColor2.Checked) return Main.Color2;
      if(rbFilterNotColor2.Checked) return int.MinValue|Main.Color2;
      if(rbFilterAndNotBlack.Checked) return int.MinValue|0x1000000|Main.Color2;
      return -1; 
    }
    public int GetDiff() {
      if(rbDiffOff.Checked) return 0;
      if(rbDiffXor.Checked) return 1;
      if(rbDiffDiff.Checked) return 2;
      if(rbDiffRed.Checked) return 3;
      if(rbDiffBW.Checked) return 4;
      if(rbDiffMin.Checked) return 5;
      if(rbDiffAvg.Checked) return 6;
      if(rbDiffMax.Checked) return 7;
      if(rbDiffEmbo.Checked) return 8;
      if(rbDiff9.Checked) return 9;
      if(rbDiff10.Checked) return 10;
      return 0;
    }
    public void SetDiff(int diff) {
      switch(diff) {
       case 1:rbDiffXor.Checked=true;break;       
       case 2:rbDiffDiff.Checked=true;break;       
       case 3:rbDiffRed.Checked=true;break;       
       case 4:rbDiffBW.Checked=true;break;       
       case 5:rbDiffMin.Checked=true;break;       
       case 6:rbDiffAvg.Checked=true;break;       
       case 7:rbDiffMax.Checked=true;break;       
       case 8:rbDiffEmbo.Checked=true;break;       
       case 9:rbDiff9.Checked=true;break;
       case 10:rbDiff10.Checked=true;break;       
       default:rbDiffOff.Checked=true;break;
      }
      
    }
    public bool GetIconSize(int w,int h,out int iw,out int ih) {
      int width,height;
      int.TryParse(tIconWidth.Text,out width);
      if(!int.TryParse(tIconHeight.Text,out height)&&width>0) {        
        height=width;
      }
      if(rIconExact.Checked) {
        w=width;h=height;
      } else if(rIcon1to1.Checked) {
        ;
      } else if(rIconWidth.Checked) {
        h=h*width/w;w=width;
      } else if(rIconHeight.Checked) {
        w=w*height/h;h=height;
      } else {
        int w2=w*height/h;
        if(w2<width) {w=w2;h=height;}
        else {h=h*width/w;w=width;}
			}
      iw=w;ih=h;
      return iw>0&&ih>0;
    }
    public int GetIconTrColor(bool force) {
      return rIconTrWhite.Checked?bmap.White:rIconTrColor2.Checked||force?Main.Color2:-1;
    }
    public int GetPrintMulti() {
      if(rPrint2x1.Checked) return 2;
      if(rPrint3x1.Checked) return 3;
      if(rPrint2x2.Checked) return 4;
      if(rPrint3x2.Checked) return 6;
      return 1;
    }
    public int GetReplace(char op) {
      if(op=='F'&&!DrawReplace.Checked) return 0;
      if(op=='S'&&!chSearchReplace.Checked) return 0;
      if(rReplace1.Checked) return 1;
      if(rReplace2.Checked) return 2;
      if(rReplace3.Checked) return 3;
      if(rReplace4.Checked) return 4;
      if(rReplace5.Checked) return 5;
      if(rReplace6.Checked) return 6;
      return 0;
    }

    private void NewSize_TextChanged(object sender, EventArgs e)
    {
      int w,h;
      int.TryParse(tNewWidth.Text,out w);
      int.TryParse(tNewHeight.Text,out h);
      if(w<1) w=1;if(h<1) h=1;
      tPixels.Text=(w*h).ToString("#,#");
    }
    public int GetFillDiff(out int diff) {
      int.TryParse(tFillDiff.Text,out diff);
      if(diff<1) diff=1;
      return BWMin.Checked?0:BWMax.Checked?3:BWSQR.Checked?2:1;
    }

    private void bcxx_Click(object sender, EventArgs e) {
      Main.ChooseColor(sender as Button);
    }

    private void fillPattCheckChanged(object sender, EventArgs e) {
      Main.Pattern.MX=fillPatMX.Checked;
      Main.Pattern.MY=fillPatMY.Checked;
      Main.Pattern.HX=fillPatHX.Checked;
      Main.Pattern.HY=fillPatHY.Checked;
    }

  }
}
