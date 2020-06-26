using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;

namespace MuMaxViewer
{
    public delegate Vector vField(Vector v);
    public partial class Form1 : Form
    {
        public vField BField;
        MuMaxTable mt;
        public int currImgNum;
        SimController ActiveSim;
        public double currXVal = 0;
        int componentNum=3;
        string Simulation_Method;
        VerticalLineAnnotation VA;
        public Form1()
        {
            InitializeComponent();
        }
        

        private string fname(int numzeroes, string baseName, int FileNum)
        {
            string fileName = baseName;
            for (int i = 0; i < (numzeroes - FileNum.ToString().Length); i++)
            {
                fileName += "0";
            }
            return fileName + FileNum.ToString() + ".ovf";
        }
        private Bitmap GetImageDiff(ovf2 fileA, ovf2 fileB)
        {
            for (int i = 0; i < fileA.data.Count; i++)
            {
                Vector v1 = fileA.data[i];
                Vector v2 = fileB.data[i];
                Vector v = v1 - v2;
                fileA.data[i] = v;
            }
            
            Bitmap bmp = Viz.MakeImage(fileA, componentNum);
            return bmp;
        }

        private ovf2 average(string baseDir, int n, int AvgNum)
        {
            string fname = baseDir + "\\" + n.ToString() + "-f1-";
            List<ovf2> ovfs = new List<ovf2>();
            for (int i = 0; i < AvgNum; i++)
            {
                ovf2 ovf = new ovf2(fname+i.ToString() + ".ovf");
                //return ovf;
                ovfs.Add(ovf);
            }
            
            double dblAvgNum = Convert.ToDouble(AvgNum);
            for (int i = 0; i < ovfs[0].data.Count; i++)
            {
                Vector avg = new Vector(0, 0, 0);
                for (int j = 0; j < AvgNum; j++)
                {
                    avg += ovfs[j].data[i];
                }
                ovfs[0].data[i] = new Vector(avg.x / dblAvgNum, avg.y / dblAvgNum, avg.z / dblAvgNum);
            }
            return ovfs[0];
        }


        private void PlotHA(string fileLoc)
        {
            chart1.Series[0].Points.Clear();
            System.IO.StreamReader sr = new System.IO.StreamReader(fileLoc);
            string line = "";
            while ((line = sr.ReadLine()) != null)
            {
                string[] spl = line.Split(new string[] { "," }, StringSplitOptions.RemoveEmptyEntries);
                double xx = Convert.ToDouble(spl[0]);
                double yy = Convert.ToDouble(spl[1]);
                chart1.Series[0].Points.AddXY(xx, yy);
            }
            sr.Close();
        }
        private void AverageMagnetizations(string dirPath, int NumOfAvgs)
        {
            //string fName = "-f1-";
            MuMaxTable mt1 = new MuMaxTable(dirPath + "\\table.txt");
            string fileNname = tb_colname.Text.Split(',')[0];
            int fnum = mt1.data[fileNname].Count;
            for (int i = 0; i < fnum; i++)
            {

                if (!System.IO.File.Exists(dirPath + "\\" + i.ToString() + "-mf.ovf"))
                {
                    ovf2 newAvg = average(dirPath, i, NumOfAvgs);
                    newAvg.SaveAs(dirPath + "\\" + i.ToString() + "-mf.ovf");
                    Console.WriteLine("Done with {0}", i);
                }
                else
                    Console.WriteLine("Skipping {0}", i);
  
            }
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            comboBox1.SelectedItem = "8Avg_FieldScan";
        }


        private void bt_StartSim_Click(object sender, EventArgs e)
        {
            string idir = tb_Dir.Text;
            StartSim(idir);
        }

        private void StartSim(string idir)
        {

            chart1.Series[0].ChartType = SeriesChartType.Line;

            System.Diagnostics.Stopwatch stw = new System.Diagnostics.Stopwatch();
            stw.Start();

            string mom_str = tb_DipMom.Text;
            string pos_str = tb_DipPos.Text;
            string[] mom = mom_str.Split(new string[] { "," }, StringSplitOptions.RemoveEmptyEntries);
            string[] pos = pos_str.Split(new string[] { "," }, StringSplitOptions.RemoveEmptyEntries);
            Vector M = new Vector(mom[0],mom[1],mom[2]);
            Vector P = new Vector(pos[0], pos[1], pos[2]);
            Dipole dip1 = new Dipole(P, M);
            double interfaceX = Convert.ToDouble(tb_IP.Text) * 1e-6;
            double MS1 = Convert.ToInt32(tb_Ms.Text)/ (4 * Math.PI) * 1.0e3;
            double MS2 = Convert.ToInt32(tb_Ms2.Text) / (4 * Math.PI) * 1.0e3;
            List<int> PltChoice = new List<int>() { 0, 1 };
            if (cb_Fz.Checked==false)
            {
                if(cb_Fx.Checked)
                {
                    PltChoice[1] = 2;
                }
                else if(cb_Fy.Checked)
                {
                    PltChoice[1] = 3;
                }
                else if(cb_Mz.Checked)
                {
                    PltChoice[0] = 1;
                    PltChoice[1] = 1;
                }
                else if (cb_Mx.Checked)
                {
                    PltChoice[0] = 1;
                    PltChoice[1] = 2;
                }
                else if(cb_My.Checked)
                {
                    PltChoice[0] = 1;
                    PltChoice[1] = 3;
                }
            }
            if (Simulation_Method=="8Avg_FieldScan")
            {
                AverageMagnetizations(idir, 8);
                pictureBox1.SizeMode = PictureBoxSizeMode.StretchImage;
                chart1.Series[0].ChartType = SeriesChartType.Line;
                currImgNum = 524;

                string loc = idir;
                chart1.Series[0].ChartType = SeriesChartType.Line;

                //FMRFM fmr = new FMRFM(dip1, new ovf2(loc + @"\1-m0.ovf"), 135282);
                FMRFM fmr = new FMRFM(dip1);
                SimController sim = new SimController(loc, fmr);
                //sim = new SimController(loc, fmr);
                
                sim.DoSweep(ops.newF, PltChoice, (vb1) => { if (vb1.x > interfaceX) { return MS2; } else { return MS1; } }, tb_section.Text);
                sim.PlotSweep(chart1, 0, "f2-2");

                sim.SaveSweep(loc + "\\fieldSweep.txt");
                ActiveSim = sim;
                mt = sim.table;

                return;
            }
            else if(Simulation_Method=="FFT")
            {
                pictureBox1.SizeMode = PictureBoxSizeMode.StretchImage;
                chart1.Series[0].ChartType = SeriesChartType.Line;
                currImgNum = 524;

                string loc = tb_Sweeps.Text;
                chart1.Series[0].ChartType = SeriesChartType.Line;

                FMRFM fmr = new FMRFM(dip1);
                SimController sim = new SimController(loc, fmr);
                sim = new SimController(loc, fmr);
                sim.DoSweep(ops.noRefF, PltChoice, (vb1) => { if (vb1.x > interfaceX) { return MS2; } else { return MS1; } },tb_section.Text);
                sim.PlotSweep(chart1, 0, "f2-2");

                sim.SaveSweep(loc + "\\fieldSweep.txt");
                ActiveSim = sim;
                mt = sim.table;

                ChartArea CA = chart1.ChartAreas[0];
                VA = new VerticalLineAnnotation();
                VA.AxisX = CA.AxisX;
                VA.AllowMoving = true;
                VA.IsInfinitive = true;
                VA.ClipToChartArea = CA.Name;
                VA.Name = "myLine";
                VA.LineColor = Color.Red;
                VA.LineWidth = 2;         // use your numbers!
                chart1.Annotations.Add(VA);

                return;
            }
            

        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            this.Simulation_Method = comboBox1.SelectedItem.ToString();
        }

        private void chart1_MouseDown(object sender, MouseEventArgs e)
        {
            double xval = chart1.ChartAreas[0].AxisX.PixelPositionToValue(e.X);
            Field.Text = Convert.ToString(xval);
            currXVal = xval;
            Console.WriteLine(xval);
            //return;
            if (e.Button == System.Windows.Forms.MouseButtons.Left)
            {
                string fileNname = tb_colname.Text.Split(',')[0];
                string fieldname = tb_colname.Text.Split(',')[1];
                double N = mt.find(fileNname, fieldname, xval);
                if (ActiveSim != null)
                {
                    if (comboBox1.Text == "8Avg_FieldScan")
                    {
                        string baseName = ActiveSim.dir + "\\" + Convert.ToInt32(N).ToString() + "-";
                        //ovf2 m0 = new ovf2(baseName + Ni.ToString() + ".ovf");
                        //ovf2 mf = new ovf2(baseName + Nf.ToString() + ".ovf");
                        ovf2 m0 = new ovf2(baseName + "m0.ovf");
                        ovf2 mf;
                        string itxt = tb_CompInfo.Text;
                        string[] sitxt = itxt.Split(new string[] { "," }, StringSplitOptions.RemoveEmptyEntries);
                        //ovf2 mf = new ovf2(baseName + "f1-0.ovf");
                        this.componentNum = Convert.ToInt16(sitxt[0]);
                        int tfAvg = Convert.ToInt16(sitxt[1]);
                        if (tfAvg == 1)
                            mf = new ovf2(baseName + "mf.ovf");
                        else
                            mf = new ovf2(baseName + "f1-0.ovf");

                        PlotRow(mf, m0, chart2, mf.header.ynodes / 2);
                        Bitmap bmp = GetImageDiff(mf, m0);
                        pictureBox1.Image = bmp;
                    }
                    else if (comboBox1.Text =="FFT")
                    {
                        string filename = ActiveSim.dir + "\\f" + Convert.ToInt32(N).ToString() + ".ovf";
                        ovf2 m = new ovf2(filename);
                        string itxt = tb_CompInfo.Text;
                        string[] sitxt = itxt.Split(new string[] { "," }, StringSplitOptions.RemoveEmptyEntries);
                        //ovf2 mf = new ovf2(baseName + "f1-0.ovf");
                        this.componentNum = Convert.ToInt16(sitxt[0]);
                        PlotRow(m, chart2, m.header.ynodes / 2);
                        Bitmap bmp = Viz.MakeImage(m, componentNum);
                        //pictureBox1.SizeMode = PictureBoxSizeMode.StretchImage;
                        pictureBox1.SizeMode = PictureBoxSizeMode.Zoom;
                        pictureBox1.Image = bmp;
                    }
                    
                }
                Console.WriteLine("N={0}, H={1}", N, (xval).ToString());
            }
        }

        private void PlotRow(ovf2 m, ovf2 mRef, Chart chart, int RowNumber)
        {
            chart.Series[0].ChartType = SeriesChartType.Line;
            Vector[] vecRef = mRef.GetRow(RowNumber, 0);
            Vector[] vecM = m.GetRow(RowNumber, 0);
            chart.Series[0].Points.Clear();
            for (int i = 0; i < vecM.Count(); i++)
            {
                double dV = 0;
                if (componentNum == 1)
                    dV = (vecM[i].x - vecRef[i].x);
                else if (componentNum == 2)
                    dV = (vecM[i].y - vecRef[i].y);
                else if (componentNum == 3)
                    dV = (vecM[i].z - vecRef[i].z);
                chart.Series[0].Points.AddXY(i, dV);
            }
        }
        private void PlotRow(ovf2 m, Chart chart, int RowNumber)
        {
            chart.Series[0].ChartType = SeriesChartType.Line;
            Vector[] vecM = m.GetRow(RowNumber, 0);
            chart.Series[0].Points.Clear();
            for (int i = 0; i < vecM.Count(); i++)
            {
                double dV = 0;
                if (componentNum == 1)
                    dV = (vecM[i].x);
                else if (componentNum == 2)
                    dV = (vecM[i].y);
                else if (componentNum == 3)
                    dV = (vecM[i].z);
                chart.Series[0].Points.AddXY(i, dV);
            }
        }
        private void btn_Browse_Click(object sender, EventArgs e)
        {
            FolderBrowserDialog fbd = new FolderBrowserDialog();
            fbd.SelectedPath = @"D:\Chris_Hammel\data\Simulations\Mumax3";
            if (fbd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                tb_Dir.Text = fbd.SelectedPath;
                lb_Files.Items.Clear();
                var files = (new System.IO.DirectoryInfo(fbd.SelectedPath)).GetFiles("*.ovf");
                foreach (var fil in files)
                {
                    lb_Files.Items.Add(fil.Name);
                }
            }
        }

        private void lb_Files_SelectedIndexChanged(object sender, EventArgs e)
        {
            string fName = tb_Dir.Text + "\\";
            if (lb_Files.SelectedIndex > -1)
            {
                fName += lb_Files.SelectedItem.ToString();
                ovf2 ovf = new ovf2(fName);
                string itxt = tb_CompInfo.Text;
                string[] sitxt = itxt.Split(new string[] { "," }, StringSplitOptions.RemoveEmptyEntries);
                //ovf2 mf = new ovf2(baseName + "f1-0.ovf");
                this.componentNum = Convert.ToInt16(sitxt[0]);
                PlotRow(ovf, chart2, ovf.header.ynodes / 2);
                Bitmap bmp = Viz.MakeImage(ovf, componentNum);
                pictureBox1.SizeMode = PictureBoxSizeMode.StretchImage;
                pictureBox1.Image = bmp;
            }
            
        }


        private void lb_Sweeps_SelectedIndexChanged(object sender, EventArgs e)
        {
            string fName = tb_Sweeps.Text + "\\";
            if (lb_Sweeps.SelectedIndex > -1)
            {
                fName += lb_Sweeps.SelectedItem.ToString();
                ovf2 ovf = new ovf2(fName);
                string itxt = tb_CompInfo.Text;
                string[] sitxt = itxt.Split(new string[] { "," }, StringSplitOptions.RemoveEmptyEntries);
                //ovf2 mf = new ovf2(baseName + "f1-0.ovf");
                this.componentNum = Convert.ToInt16(sitxt[0]);
                PlotRow(ovf,  chart2, ovf.header.ynodes / 2);
                Bitmap bmp = Viz.MakeImage(ovf, componentNum);
                //pictureBox1.SizeMode = PictureBoxSizeMode.StretchImage;
                pictureBox1.SizeMode = PictureBoxSizeMode.Zoom;
                pictureBox1.Image = bmp;
                if(mt!=null)
                {
                    string filename = lb_Sweeps.SelectedItem.ToString();
                    string fn = filename.Substring(1, filename.Length - 5);
                    int fileN = Convert.ToInt32(fn);
                    string fileNname = tb_colname.Text.Split(',')[0];
                    string fieldname = tb_colname.Text.Split(',')[1];
                    int index = mt.data[fileNname].IndexOf(fileN);
                    double field = mt.data[fieldname][index];
                    double amp = chart1.Series[0].Points.Where(point => point.XValue == field).ToList()[0].YValues[0];
                    
                    this.VA.X = field;
                    Field.Text = field.ToString() + "(G)";
                    Amp.Text = amp.ToString();
                }
                
            }
        }
        
        private void ExportCurrFile(System.IO.StreamWriter sw, ovf2 m0, ovf2 mf,bool inseries,int index)
        {
            double factor = 1.0;
            int Nx = m0.header.xnodes;
            int Ny = m0.header.ynodes;

            int startK = 0;
            int endK = (m0.GetRow(0, 0)).Count();
            
            double vcomp = 0;
            if(inseries)
            {
                if(index>0&&index<Convert.ToInt32(tb_packn.Text))
                {
                    sw.Write("##\n");
                }
            }
            for (int i = 0; i < Ny; i++)
            {
                Vector[] vrow0 = m0.GetRow(i, 0);
                Vector[] vrowf;
                if (mf != null)
                    vrowf = mf.GetRow(i, 0);
                else
                    vrowf = new Vector[vrow0.Count()];
                Vector[] vsub = new Vector[vrow0.Count()];
                int colStart = vrow0.Count() / 2;
                for (int k = startK; k < endK; k++)
                {
                    if (mf != null)
                        vsub[k] = vrow0[k] - vrowf[k];
                    else
                        vsub[k] = vrow0[k];
                    //which component to plot (x, y or z)
                    string itxt = tb_CompInfo.Text;
                    string[] sitxt = itxt.Split(new string[] { "," }, StringSplitOptions.RemoveEmptyEntries);
                    //ovf2 mf = new ovf2(baseName + "f1-0.ovf");
                    this.componentNum = Convert.ToInt16(sitxt[0]);
                    if(this.componentNum==1)
                    {
                        vcomp = vsub[k].x;
                    }
                    else if(this.componentNum == 2)
                    {
                        vcomp = vsub[k].y;
                    }
                    else if (this.componentNum == 3)
                    {
                        vcomp = vsub[k].z;
                    }
                    
                    sw.Write((factor * vcomp).ToString());
                    if (k < endK - 1)
                        sw.Write(" ");
                    else
                        sw.Write("\n");
                }

            }
            sw.Close();
            
        }
        private void btn_Export_Click(object sender, EventArgs e)
        {
            SaveFileDialog svd = new SaveFileDialog();
            if (svd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {

                Console.WriteLine(svd.FileName);
                string FileName = svd.FileName.Substring(0, svd.FileName.Length - 4) + ".txt";
                System.IO.StreamWriter sw = new System.IO.StreamWriter(FileName);
                if (ActiveSim != null)
                {
                    //find the index N of currently selected point in the table
                    string fileNname = tb_colname.Text.Split(',')[0];
                    string fieldname = tb_colname.Text.Split(',')[1];
                    double N = mt.find(fileNname, fieldname, currXVal);
                    string baseName = ActiveSim.dir + "\\" + Convert.ToInt32(N).ToString() + "-";
                    ovf2 m0 = new ovf2(baseName + "m0.ovf");
                    //ovf2 mf = new ovf2(baseName + "f1-" + k.ToString() + ".ovf");
                    //ovf2 mf = new ovf2(baseName + "f1-0.ovf", cb_py.Checked);
                    ovf2 mf = new ovf2(baseName + "mf.ovf");
                    string itxt = tb_CompInfo.Text;
                    string[] sitxt = itxt.Split(new string[] { "," }, StringSplitOptions.RemoveEmptyEntries);
                    //ovf2 mf = new ovf2(baseName + "f1-0.ovf");
                    this.componentNum = Convert.ToInt16(sitxt[0]);
                    int tfAvg = Convert.ToInt16(sitxt[1]);
                    if (tfAvg == 1)
                    {
                        mf = new ovf2(baseName + "mf.ovf");
                        ExportCurrFile(sw, m0, mf, false, 0);
                    }
                    else if (tfAvg == 2)
                    {
                        for (int k = 0; k < 8; k++)
                        {
                            string FName = svd.FileName.Substring(0, svd.FileName.Length - 4) + "-" + "-snapshot" + k.ToString() + ".txt";
                            System.IO.StreamWriter sww = new System.IO.StreamWriter(FName);
                            mf = new ovf2(baseName + "f1-" + k.ToString() + ".ovf");
                            ExportCurrFile(sww, m0, mf, false, 0);
                        }
                    }
                    else
                    {
                        mf = new ovf2(baseName + "f1-0.ovf");
                        ExportCurrFile(sw, m0, mf, false, 0);
                    }
                }
                sw.Close();

            }
        }
        private void btn_ExportAll_Click(object sender, EventArgs e)
        {
            int i = 0;
            int packagenum = 0;
            string dir = tb_Sweeps.Text;
            //ovf2 m0 = new ovf2(dir + "\\m000000.ovf", false);
            
            foreach (string fname in lb_Sweeps.Items)
            {
                string FileName = "";
                if(!cb_inseries.Checked)
                {
                    FileName = dir + '\\' + fname.Substring(0, fname.Length - 4) + ".txt";
                }
                else
                {
                    FileName = dir + "\\package" + Convert.ToString(packagenum) + ".txt";
                }
                System.IO.StreamWriter sw = new System.IO.StreamWriter(FileName,cb_inseries.Checked);
                ovf2 m = new ovf2(dir + '\\' + fname);

                if (tb_minit.Text != "")
                {
                    ovf2 m0 = new ovf2(tb_minit.Text);
                    ExportCurrFile(sw, m, m0, cb_inseries.Checked, i);
                }
                //find ground state automatically if the selected states are generated using 8-avg methods 
                else if (cb_autoground.Checked)
                {
                    string f0name = fname.Split('-')[0] + "-m0.ovf";
                    ovf2 m0 = new ovf2(dir + '\\' + f0name); ;
                    ExportCurrFile(sw, m, m0, cb_inseries.Checked, i);
                }
                else
                { ExportCurrFile(sw, m, null, cb_inseries.Checked, i); }
                
                i++;
                if(i==Convert.ToInt32(tb_packn.Text))
                {
                    i = 0;
                    packagenum++;
                }
                sw.Close();
            }

        }
        private void btn_ExportSel_Click(object sender, EventArgs e)
        {
            string dir = tb_Sweeps.Text;
            //ovf2 m0 = new ovf2(dir + "\\m000000.ovf", false);
            string fname = lb_Sweeps.SelectedItem.ToString();
            string FileName = dir + '\\' + fname.Substring(0, fname.Length - 4) + ".txt";
            System.IO.StreamWriter sw = new System.IO.StreamWriter(FileName);
            ovf2 m = new ovf2(dir + '\\' + fname);

            if (tb_minit.Text != "")
            {
                ovf2 m0 = new ovf2(tb_minit.Text);
                ExportCurrFile(sw, m0, m, cb_inseries.Checked, 0);
            }
            //find ground state automatically if the selected states are generated using 8-avg methods 
            else if (cb_autoground.Checked)
            {
                string f0name = fname.Split('-')[0] + "-m0.ovf";
                ovf2 m0 = new ovf2(dir + '\\' + f0name); ;
                ExportCurrFile(sw, m0, m, cb_inseries.Checked, 0);
            }
            else
            { ExportCurrFile(sw, m, null, cb_inseries.Checked, 0); }
            sw.Close();

        }
        private void btn_BrowseSweeps_Click(object sender, EventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Multiselect = true;
            ofd.InitialDirectory = @"Y:\3-2016\LSA236-Initial\Angular Dependence\LSA236-7";
            ofd.Filter = "Text|*.ovf";
            if (ofd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                if (ofd.FileNames.Count() == 0)
                    return;
                System.IO.FileInfo fi = new System.IO.FileInfo(ofd.FileNames[0]);
                tb_Sweeps.Text = fi.DirectoryName;
                lb_Sweeps.Items.Clear();
                List<string> lines = new List<string>();
                foreach (string fname in ofd.FileNames)
                {
                    System.IO.FileInfo fileI = new System.IO.FileInfo(fname);
                    lb_Sweeps.Items.Add(fileI.Name);
                }
            }
        }

        private void cb_Fx_CheckedChanged(object sender, EventArgs e)
        {
            if(cb_Fx.Checked)
            {
                cb_Fz.Checked = false;
                cb_Mx.Checked = false;
                cb_Mz.Checked = false;
            }
        }

        private void cb_Fz_CheckedChanged(object sender, EventArgs e)
        {
            if (cb_Fz.Checked)
            {
                cb_Fx.Checked = false;
                cb_Mx.Checked = false;
                cb_Mz.Checked = false;
            }
        }

        private void cb_Mx_CheckedChanged(object sender, EventArgs e)
        {
            if (cb_Mx.Checked)
            {
                cb_Fz.Checked = false;
                cb_Fx.Checked = false;
                cb_Mz.Checked = false;
            }
        }

        private void cb_Mz_CheckedChanged(object sender, EventArgs e)
        {
            if (cb_Mz.Checked)
            {
                cb_Fx.Checked = false;
                cb_Mx.Checked = false;
                cb_Fz.Checked = false;
            }
        }

        private void btn_minit_Click(object sender, EventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Multiselect = false;
            ofd.InitialDirectory = @"Y:\3-2016\LSA236-Initial\Angular Dependence\LSA236-7";
            ofd.Filter = "Text|*.ovf";
            if (ofd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                if (ofd.FileNames.Count() == 0)
                    return;
                System.IO.FileInfo fi = new System.IO.FileInfo(ofd.FileNames[0]);
                tb_minit.Text = fi.FullName;
            }
        }

        
    }

    public class Dipole
    {
        public Vector r0;   //position
        public Vector m;    //moment
        public Dipole(Vector origin, Vector moment)
        {
            this.r0 = origin;
            this.m = moment;
        }

        //SI units
        public Vector B(Vector r)
        {
            double c = 1e-7;
            Vector dr = new Vector(r.x - r0.x, r.y - r0.y, r.z - r0.z);
            double c1 = 3 *c* m.dot(dr) / Math.Pow(dr.norm(), 5.0);
            double c2 = -c / Math.Pow(dr.norm(), 3.0);
            Vector bfield = new Vector(c1 * dr.x + c2 * m.x, c1 * dr.y + c2 * m.y, c1 * dr.z + c2 * m.z);
            return bfield;

        }

        public double Fz(Vector dM, Vector rm)
        {
            double fz = this.m.z * Bz_z(dM, rm);
            return fz;
        }

        public double Fz_ip(Vector dM, Vector rm)
        {
            double fz = this.m.x * Bx_z(dM, rm);
            return fz;
        }
        public double Fz_ipy(Vector dM, Vector rm)
        {
            double fz = this.m.y * Bx_z(dM, rm);
            return fz;
        }
        //ri is position of elemtn dM
        public double Bz_z(Vector dM, Vector ri)
        {
            double c = 1e-7;
            double mz = dM.z;
            double my = dM.y;
            double mx = dM.x;
            double rsq = Math.Pow((r0.x - ri.x), 2) + Math.Pow((r0.y - ri.y), 2) + Math.Pow((r0.z - ri.z), 2);
            double term1 = 3 * (mx * (r0.x - ri.x) + my * (r0.y - ri.y) + mz * (r0.z - ri.z)) / Math.Pow(rsq, 5.0 / 2);
            double term2 = 6 * mz * (r0.z - ri.z) / Math.Pow(rsq, 5.0 / 2);
            double term3 = -15 * (mx * (r0.x - ri.x) + my * (r0.y - ri.y) + mz * (r0.z - ri.z)) * Math.Pow(r0.z - ri.z, 2) / Math.Pow(rsq, 7.0 / 2);
            double sum = c*(term1 + term2 + term3);
            return sum;
        }

        public double Bx_z(Vector dM, Vector ri)
        {
            double c = 1e-7;
            double mz = dM.z;
            double my = dM.y;
            double mx = dM.x;
            double rsq = Math.Pow((r0.x - ri.x), 2) + Math.Pow((r0.y - ri.y), 2) + Math.Pow((r0.z - ri.z), 2);
            double term1 = 3 * (mx * (r0.x - ri.x) + my * (r0.y - ri.y) + mz * (r0.z - ri.z)) / Math.Pow(rsq, 5.0 / 2);
            double term2 = 6 * mx * (r0.x - ri.x) / Math.Pow(rsq, 5.0 / 2);
            double term3 = -15 * (mx * (r0.x - ri.x) + my * (r0.y - ri.y) + mz * (r0.z - ri.z)) * Math.Pow(r0.x - ri.x, 2) / Math.Pow(rsq, 7.0 / 2);
            double sum = c * (term1 + term2 + term3);
            return sum;
        }
    }

    public class HalfRadiationInfo
    {
        public string fileLoc;
        public double interfaceLocation;

        public HalfRadiationInfo(string fileLoc, double interfaceLocation)
        {
            this.fileLoc = fileLoc;
            this.interfaceLocation = interfaceLocation;
        }
    }

    public class MuMaxTable
    {
        public string fileLoc;
        public Dictionary<string, List<double>> data;
        public List<string> keys;
        public MuMaxTable(string fileLoc)
        {
            this.fileLoc = fileLoc;
            data = new Dictionary<string, List<double>>();
            LoadFile(fileLoc);
        }

        public void LoadFile(string fileLoc)
        {
            data = new Dictionary<string, List<double>>();
            System.IO.StreamReader sr = new System.IO.StreamReader(fileLoc);
            string line = sr.ReadLine();
            line = line.Replace("#", "");
            string[] vars = line.Split(new string[] { "\t" }, StringSplitOptions.RemoveEmptyEntries);
            this.keys = new List<string>();
            foreach (string v in vars)
            {
                data.Add(v, new List<double>());
                keys.Add(v);
            }

            while ((line = sr.ReadLine()) != null)
            {
                vars = line.Split(new string[] { "\t" }, StringSplitOptions.RemoveEmptyEntries);
                if (vars.Count() == keys.Count)
                {
                    for (int i = 0; i < vars.Count(); i++)
                    {
                        data[keys[i]].Add(Convert.ToDouble(vars[i]));
                    }
                }
            }

            sr.Close();
            
        }

        public double find(string returnKey, string matchKey, double matchValue)
        {
            if (!(keys.Contains(returnKey) && keys.Contains(matchKey)))
            {
                Console.WriteLine("return of matchkey doesn't exist");
                return -1.23;
            }

            int N = data[returnKey].Count;
            double err = double.PositiveInfinity;
            double bestVal = 0;
            for (int i = 0; i < N; i++)
            {
                if (Math.Abs(data[matchKey][i] - matchValue) < err)
                {
                    err = Math.Abs(data[matchKey][i] - matchValue);
                    bestVal = data[returnKey][i];
                }
            }
            return bestVal;
        }
        private double[] findMinMax(string var)
        {
            double min = double.PositiveInfinity;
            double max = double.NegativeInfinity;
            List<double> xs = data[var];
            for (int i = 0; i < xs.Count; i++)
            {
                if (xs[i] < min)
                    min = xs[i];
                if (xs[i] > max)
                    max = xs[i];
            }
            return new double[] { min, max };
        }

        
        public void Plot(Chart c, int seriesIndex, string xVar, string yVar)
        {
            if (c.Series.Count - 1 > seriesIndex)
            {
                Console.WriteLine("That dont exist");
                return;
            }

            if(!(data.ContainsKey(xVar) && data.ContainsKey(yVar)))
                return;

            while (c.Series.Count - 1 < seriesIndex)
                c.Series.Add(new Series());
            c.Series[seriesIndex].Points.Clear();

            List<double> xs = data[xVar];
            List<double> ys = data[yVar];
            for (int i = 0; i < xs.Count; i++)
            {
                c.Series[seriesIndex].Points.AddXY(xs[i], ys[i]);
            }

            double[] minmax = findMinMax(yVar);
            c.ChartAreas[0].AxisY.Minimum = minmax[0];
            c.ChartAreas[0].AxisY.Maximum = minmax[1];

        }

    }

    public class Vector
    {
        public double x;
        public double y;
        public double z;

        public Vector(double x, double y, double z)
        {
            this.x = x;
            this.y = y;
            this.z = z;
        }

        public Vector(string x, string y, string z)
        {
            this.x = Convert.ToDouble(x);
            this.y = Convert.ToDouble(y);
            this.z = Convert.ToDouble(z);
        }

        public double norm()
        {
            return Math.Sqrt(x * x + y * y + z * z);
        }
        public double normxy()
        {
            return Math.Sqrt(x * x + y * y);
        }
        public double dot(Vector v)
        {
            return v.x * x + v.y * y + v.z * z;
        }
        public double anglexy()
        {
            return Math.Atan2(y, x);
        }
        public static Vector operator -(Vector a, Vector b)
        {
            return new Vector(a.x - b.x, a.y - b.y, a.z - b.z);
        }

        public static Vector operator +(Vector a, Vector b)
        {
            return new Vector(a.x + b.x, a.y + b.y, a.z + b.z);
        }

        public override string ToString()
        {
            return String.Format("<{0},{1},{2}>", this.x, this.y, this.z);
        }
    }
}
