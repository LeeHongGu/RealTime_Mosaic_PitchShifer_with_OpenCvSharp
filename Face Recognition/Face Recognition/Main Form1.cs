using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;


using OpenCvSharp;
using OpenCvSharp.UserInterface;


using System.IO;
using System.Xml;
using System.Runtime.InteropServices;
using System.Threading;
using System.Security.Principal;
using System.Threading.Tasks;
using Microsoft.Win32.SafeHandles;


namespace Face_Recognition
{
    public partial class Form1 : Form
    {
        #region variables
        Mat currentFrame = new Mat(); //웹캠에서 얻은 현재 이미지
        Mat result, TrainedFace = new Mat(); //결과 이미지와 훈련된 얼굴을 저장하는 데 사용
        Mat gray_frame = new Mat(); //opencv 연산을 위해 웹캠에서 얻은 grayscale 이미지

        VideoCapture grabber; //웹캠 variable

        public CascadeClassifier Face = new CascadeClassifier(Application.StartupPath + "/Cascades/haarcascade_frontalface_alt.xml");//face detection method 

        //Classifier with default training location
        Classifier_Train Eigen_Recog = new Classifier_Train();

        #endregion

        public Form1()
        {
            InitializeComponent();

            this.StartPosition = FormStartPosition.Manual;
            this.Location = new System.Drawing.Point(150, 180);

            //각 이미지에 대해 이전에 훈련된 face and label
            if (Eigen_Recog.IsTrained)
            {
                message_bar.Text = "Training Data loaded";
            }
            else
            {
                message_bar.Text = "No training data found, please train program using Train menu option";
            }
            initialise_capture();

        }

        //Open training form and pass this
        private void trainToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //Stop Camera
            stop_capture();

            //OpenForm
            Training_Form TF = new Training_Form(this);
            TF.Show();
        }
        public void retrain()
        {
            Console.WriteLine("main form retrain");
            Eigen_Recog = new Classifier_Train();
            if (Eigen_Recog.IsTrained)
            {
                message_bar.Text = "Training Data loaded";
            }
            else
            {
                message_bar.Text = "No training data found, please train program using Train menu option";
            }
        }

        //Camera Start Stop
        public void initialise_capture()
        {
            grabber = new VideoCapture(0);
            grabber.FrameWidth = 640;
            grabber.FrameHeight = 480;


            //Initialize the FrameGraber event
            if (parrellelToolStripMenuItem.Checked)
            {
                Application.Idle += new EventHandler(FrameGrabber_Parrellel);
            }
            else
            {
                Application.Idle += new EventHandler(FrameGrabber_Standard);
            }
        }
        private void stop_capture()
        {
            if (parrellelToolStripMenuItem.Checked)
            {
                Application.Idle -= new EventHandler(FrameGrabber_Parrellel);
            }
            else
            {
                Application.Idle -= new EventHandler(FrameGrabber_Standard);
            }
            if (grabber != null)
            {
                grabber.Dispose();
            }
        }

        //Process Frame
        void FrameGrabber_Standard(object sender, EventArgs e)
        {
            //Get the current frame form capture device
            grabber.Read(currentFrame);
            //Clear_Faces_Found();

            if (currentFrame != null)
            {
                //Convert it to Grayscale
                Cv2.CvtColor(currentFrame, gray_frame, ColorConversionCodes.BGR2GRAY);

                //Face Detector
                Rect[] facesDetected = Face.DetectMultiScale(gray_frame, 1.2, 10, HaarDetectionTypes.DoCannyPruning, new OpenCvSharp.Size(50, 50), OpenCvSharp.Size.Zero);

                //Action for each element detected
                for (int i = 0; i < facesDetected.Length; i++)// (Rectangle face_found in facesDetected)
                {
                    //harr에 따른 얼굴에 초점을 둬서 완벽하진 않지만 배경 노이즈를 대부분 제거할 수 있는 효과가 큼
                    //얼굴 표면이 작아보일 수 있음. 이는 얼굴 식별 기능을 구현하기 위함
                    facesDetected[i].X += (int)(facesDetected[i].Height * 0.15);
                    facesDetected[i].Y += (int)(facesDetected[i].Width * 0.22);
                    facesDetected[i].Height -= (int)(facesDetected[i].Height * 0.3);
                    facesDetected[i].Width -= (int)(facesDetected[i].Width * 0.35);

                    //result = currentFrame.Copy(facesDetected[i]).Convert<Gray, byte>().Resize(100, 100, Emgu.CV.CvEnum.INTER.CV_INTER_CUBIC);
                    result = new Mat(currentFrame, facesDetected[i]);
                    Cv2.Resize(result, result, new OpenCvSharp.Size(100, 100), 0, 0, InterpolationFlags.Cubic);
                    Cv2.CvtColor(result, result, ColorConversionCodes.BGR2GRAY);
                    Cv2.EqualizeHist(result, result);
                    //draw the face detected in the ith gray channel with red color
                    Cv2.Rectangle(currentFrame, facesDetected[i], Scalar.Red, 2);

                    //If there is a learned material, execute the function below
                    if (Eigen_Recog.IsTrained)
                    {
                        string name = Eigen_Recog.Recognise(result);
                        int match_value = (int)Eigen_Recog.Get_Eigen_Distance;

                        //얼굴이 인식되지 않을 경우, Unknown
                        //Unknown -> Mosaic
                        if (name == "Unknown")
                        {
                            Mat mosaic_frame = new Mat(currentFrame, facesDetected[i]);
                            Mat temp_frame = new Mat();

                            //Cv2.Resize(mosaic_frame, temp_frame, new OpenCvSharp.Size(item.Width / ((item.X+10)/10), item.Height / ((item.Y+10)/10)));
                            Cv2.Resize(mosaic_frame, temp_frame, new OpenCvSharp.Size(facesDetected[i].Width / 15, facesDetected[i].Height / 15));
                            Cv2.Resize(temp_frame, mosaic_frame, new OpenCvSharp.Size(facesDetected[i].Width, facesDetected[i].Height));
                        }

                        //Draw the label for each face detected and recognized
                        Cv2.PutText(currentFrame, name + " ", new OpenCvSharp.Point(facesDetected[i].X - 2, facesDetected[i].Y - 2), HersheyFonts.HersheyComplex, 1, Scalar.LightGreen);

                        ADD_Face_Found(result, name, match_value);
                    }
                }
                //Show the faces procesed and recognized
                image_PICBX.ImageIpl = currentFrame;
            }
        }

        void FrameGrabber_Parrellel(object sender, EventArgs e)
        {
            //Get the current frame form capture device
            grabber.Read(currentFrame);

            Cv2.Resize(currentFrame, currentFrame, new OpenCvSharp.Size(640, 480), 0, 0, InterpolationFlags.Cubic);

            if (currentFrame != null)
            {
                gray_frame = new Mat();
                //Convert it to Grayscale                             
                Cv2.CvtColor(currentFrame, gray_frame, ColorConversionCodes.BGR2GRAY);
                //Clear_Faces_Found();

                //Face Detector
                Rect[] facesDetected = Face.DetectMultiScale(gray_frame, 1.2, 10, HaarDetectionTypes.DoCannyPruning, new OpenCvSharp.Size(50, 50), OpenCvSharp.Size.Zero);

                //Action for each face element detected 
                //Parallel Threading task..
                Parallel.For(0, facesDetected.Length, i =>
                {
                    try
                    {
                        facesDetected[i].X += (int)(facesDetected[i].Height * 0.15);
                        facesDetected[i].Y += (int)(facesDetected[i].Width * 0.22);
                        facesDetected[i].Height -= (int)(facesDetected[i].Height * 0.3);
                        facesDetected[i].Width -= (int)(facesDetected[i].Width * 0.35);

                        result = new Mat(currentFrame, facesDetected[i]);
                        Cv2.Resize(result, result, new OpenCvSharp.Size(100, 100), 0, 0, InterpolationFlags.Cubic);
                        Cv2.CvtColor(result, result, ColorConversionCodes.BGR2GRAY);
                        Cv2.EqualizeHist(result, result);

                        //draw the face detected in the ith gray channel with red color
                        Cv2.Rectangle(currentFrame, facesDetected[i], Scalar.Red, 2);

                        //If there is a learned material, execute the function below
                        if (Eigen_Recog.IsTrained)
                        {
                            string name = Eigen_Recog.Recognise(result);
                            int match_value = (int)Eigen_Recog.Get_Eigen_Distance;

                            //얼굴이 인식되지 않을 경우, Unknown
                            //Unknown -> Mosaic
                            if (name == "Unknown")
                            {
                                Mat mosaic_frame = new Mat(currentFrame, facesDetected[i]);
                                Mat temp_frame = new Mat();

                                Cv2.Resize(mosaic_frame, temp_frame, new OpenCvSharp.Size(facesDetected[i].Width / 15, facesDetected[i].Height / 15));
                                Cv2.Resize(temp_frame, mosaic_frame, new OpenCvSharp.Size(facesDetected[i].Width, facesDetected[i].Height));
                            }

                            //Draw the label for each face detected and recognized
                            Cv2.PutText(currentFrame, name + " ", new OpenCvSharp.Point(facesDetected[i].X - 2, facesDetected[i].Y - 2), HersheyFonts.HersheyComplex, 1, Scalar.LightGreen);
                            ADD_Face_Found(result, name, match_value);
                        }

                    }
                    catch (Exception ex)
                    {
                        //parellel loop 버그
                        //thead 반납, or 쓸데없는 오류
                        Console.WriteLine(ex.ToString());
                    }
                });
                //Show the faces procesed and recognized
                //image_PICBX.Image = currentFrame.ToBitmap();
                image_PICBX.ImageIpl = currentFrame;
            }
        }

        //Add Picture box and label to a panel for each face
        int faces_count = 0;
        int faces_panel_Y = 0;
        int faces_panel_X = 0;

        void Clear_Faces_Found()
        {
            this.Faces_Found_Panel.Controls.Clear();
            faces_count = 0;
            faces_panel_Y = 0;
            faces_panel_X = 0;
        }
        void ADD_Face_Found(Mat img_found, string name_person, int match_value)
        {
            PictureBoxIpl PI = new PictureBoxIpl();
            PI.Location = new System.Drawing.Point(faces_panel_X, faces_panel_Y);
            PI.Height = 80;
            PI.Width = 80;
            PI.SizeMode = PictureBoxSizeMode.StretchImage;
            PI.Image = OpenCvSharp.Extensions.BitmapConverter.ToBitmap(img_found);
            Label LB = new Label();
            LB.Text = name_person + " " + match_value.ToString();
            LB.Location = new System.Drawing.Point(faces_panel_X, faces_panel_Y + 80);
            //LB.Width = 80;
            LB.Height = 15;

            this.Faces_Found_Panel.Controls.Add(PI);
            this.Faces_Found_Panel.Controls.Add(LB);
            faces_count++;
            if (faces_count == 2)
            {
                faces_panel_X = 0;
                faces_panel_Y += 100;
                faces_count = 0;
            }
            else faces_panel_X += 85;

            if (Faces_Found_Panel.Controls.Count > 10)
            {
                Clear_Faces_Found();
            }

        }

        //Menu Opeartions
        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.Dispose();
        }
        private void singleToolStripMenuItem_Click(object sender, EventArgs e)
        {
            parrellelToolStripMenuItem.Checked = false;
            singleToolStripMenuItem.Checked = true;
            Application.Idle -= new EventHandler(FrameGrabber_Parrellel);
            Application.Idle += new EventHandler(FrameGrabber_Standard);
        }
        private void parrellelToolStripMenuItem_Click(object sender, EventArgs e)
        {
            parrellelToolStripMenuItem.Checked = true;
            singleToolStripMenuItem.Checked = false;
            Application.Idle -= new EventHandler(FrameGrabber_Standard);
            Application.Idle += new EventHandler(FrameGrabber_Parrellel);

        }
        private void saveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SaveFileDialog SF = new SaveFileDialog();
            //얼굴 인식기에 대한 파일 식별을 위해 파일 확장자를 설정
            switch (Eigen_Recog.Recognizer_Type)
            {
                case ("OpenCvSharp.face.LBPHFaceRecognizer"):
                    SF.Filter = "LBPHFaceRecognizer File (*.LBPH)|*.LBPH";
                    break;
                case ("OpenCvSharp.face.FisherFaceRecognizer"):
                    SF.Filter = "FisherFaceRecognizer File (*.FFR)|*.FFR";
                    break;
                case ("OpenCvSharp.face.EigenFaceRecognizer"):
                    SF.Filter = "EigenFaceRecognizer File (*.EFR)|*.EFR";
                    break;
            }
            if (SF.ShowDialog() == DialogResult.OK)
            {
                Eigen_Recog.Save_Eigen_Recogniser(SF.FileName);
            }
        }
        private void loadToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenFileDialog OF = new OpenFileDialog();
            OF.Filter = "EigenFaceRecognizer File (*.EFR)|*.EFR|LBPHFaceRecognizer File (*.LBPH)|*.LBPH|FisherFaceRecognizer File (*.FFR)|*.FFR";
            if (OF.ShowDialog() == DialogResult.OK)
            {
                Eigen_Recog.Load_Eigen_Recogniser(OF.FileName);
            }
        }

        //Unknown Eigen face calibration
        private void Eigne_threshold_txtbxChanged(object sender, EventArgs e)
        {
            //고유 임계값 보정
            try
            {
                Eigen_Recog.Set_Eigen_Threshold = Math.Abs(Convert.ToInt32(Eigne_threshold_txtbx.Text));
                message_bar.Text = "Eigen Threshold Set";
            }
            catch
            {
                message_bar.Text = "Error in Threshold input please use int";
            }
        }

        private void eigenToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //Uncheck other menu items
            fisherToolStripMenuItem.Checked = false;
            lBPHToolStripMenuItem.Checked = false;

            Eigen_Recog.Recognizer_Type = "OpenCvSharp.face.EigenFaceRecognizer";
            Eigen_Recog.Retrain();
        }

        private void fisherToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //Uncheck other menu items
            lBPHToolStripMenuItem.Checked = false;
            eigenToolStripMenuItem.Checked = false;

            Eigen_Recog.Recognizer_Type = "OpenCvSharp.face.FisherFaceRecognizer";
            Eigen_Recog.Retrain();
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            Application.Exit();
        }

        private void lBPHToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //Uncheck other menu items
            fisherToolStripMenuItem.Checked = false;
            eigenToolStripMenuItem.Checked = false;

            Eigen_Recog.Recognizer_Type = "OpenCvSharp.face.LBPHFaceRecognizer";
            Eigen_Recog.Retrain();
        }
    }
}
