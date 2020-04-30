using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Diagnostics;
using Emgu.CV;
using Emgu.CV.Structure;
using System.IO;

namespace MyLaserTurret
{
	public partial class Form1 : Form
	{
        MCvFont font = new MCvFont(Emgu.CV.CvEnum.FONT.CV_FONT_HERSHEY_TRIPLEX, 0.6d, 0.6d);
        HaarCascade faceDetected;
        Image<Bgr, Byte> Frame;
        Capture camera;
        Image<Gray, byte> result;
        Image<Gray, byte> TrainedFace = null;
        Image<Gray, byte> grayFace = null;
        List<Image<Gray, byte>> trainingImages = new List<Image<Gray, byte>>();
        List<string> labels = new List<string>();
        List<string> Users = new List<string>();
        int Count, Numlables, t;
        string name, names = null;

		public Stopwatch watch { get; set; }

        public Form1()
		{
			InitializeComponent();

            faceDetected = new HaarCascade("haarcascade_frontalface_default.xml");

            try
            {
                string Labelsinf = File.ReadAllText(Application.StartupPath + "/Faces/Faces.txt");
                string[] Labels = Labelsinf.Split(',');
                Numlables = Convert.ToInt16(Labels[0]);

                Count = Numlables;
                string FacesLoad;
                for (int i = 1; i < Numlables + 1; i++)
                {
                    FacesLoad = "face" + i + ".bmp";
                    trainingImages.Add(new Image<Gray, byte>(Application.StartupPath + "/Faces/Faces.txt"));
                    labels.Add(Labels[i]);
                }
            }
            catch(Exception ex)
            {
               
            }

        }

		private void Form1_Load(object sender, EventArgs e)
		{
            //lol
        }
        private void startTracking_Click(object sender, EventArgs e)
        {
            port.PortName = richTextBox1.Text;
            try
            {
                camera = new Capture();
            }catch (Exception ex)
            {
                MessageBox.Show("Unable to open Camera, Error:\n\n" + ex);
                closeApplication();

            }

            try
            {
                port.Open();
            }
            catch (Exception ex2)
            {
                MessageBox.Show("Unable to open Serial-Port, Error:\n\n" + ex2);
                closeApplication();
            }
            watch = Stopwatch.StartNew();

            camera.QueryFrame();
            Application.Idle += new EventHandler(FrameProcedure);
        }

        private void closeApplication()
        {
            Application.Exit();
        }

        private void FrameProcedure(object sender, EventArgs e)
        {
            //Users.Add("");
            Frame = camera.QueryFrame().Resize(320, 240, Emgu.CV.CvEnum.INTER.CV_INTER_CUBIC);
            grayFace = Frame.Convert<Gray, Byte>();
            MCvAvgComp[][] facesDetectedNow = grayFace.DetectHaarCascade(faceDetected, 1.2, 10, Emgu.CV.CvEnum.HAAR_DETECTION_TYPE.DO_CANNY_PRUNING, new Size(20, 20));
            foreach (MCvAvgComp f in facesDetectedNow[0])
            {
                result = Frame.Copy(f.rect).Convert<Gray, Byte>().Resize(100, 100, Emgu.CV.CvEnum.INTER.CV_INTER_CUBIC);
                
                Frame.Draw(f.rect, new Bgr(Color.Green), 3);

                writeToPort(new Point(f.rect.X, f.rect.Y));


                if (trainingImages.ToArray().Length != 0)
                {
                    MCvTermCriteria termCriterias = new MCvTermCriteria(Count, 0.001);
                    EigenObjectRecognizer recognizer = new EigenObjectRecognizer(trainingImages.ToArray(), labels.ToArray(), 1500, ref termCriterias);
                    name = recognizer.Recognize(result);
                    Frame.Draw(name, ref font, new Point(f.rect.X - 2, f.rect.Y), new Bgr(Color.Red));


                }
                
            }
            cameraBox.Image = Frame;
            names = "";
            
        }
        public void writeToPort(Point coordinates)
        {
            if (watch.ElapsedMilliseconds > 15)
            {
                watch = Stopwatch.StartNew();

                port.Write(String.Format("X{0}Y{1}",
                    //change these values if it isnt accurate
                (180 - coordinates.X / (cameraBox.Width / 180)),
                (150 - coordinates.Y / (cameraBox.Height / 180))));
            }

        }
    }
}
