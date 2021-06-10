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
using OpenCvSharp.Face;
using OpenCvSharp.Extensions;

using System.IO;
using System.Drawing.Imaging;
using System.Xml;
using System.Threading;


namespace Face_Recognition
{
    public partial class Training_Form : Form
    {
        #region Variables
        //웹캡 variable
        VideoCapture grabber;

        //Images for finding face
        Mat currentFrame = new Mat();
        Mat result = null;
        Mat gray_frame = new Mat();

        //Classifier
        CascadeClassifier Face;

        //For aquiring 10 images in a row
        List<Mat> resultImages = new List<Mat>();
        int results_list_pos = 0;
        int num_faces_to_aquire = 10;
        bool RECORD = false;

        //Saving jpg
        List<Mat> ImagestoWrite = new List<Mat>();
        EncoderParameters ENC_Parameters = new EncoderParameters(1);
        EncoderParameter ENC = new EncoderParameter(System.Drawing.Imaging.Encoder.Quality, 100);
        ImageCodecInfo Image_Encoder_JPG;

        //Saving XAML Data file
        List<string> NamestoWrite = new List<string>();
        List<string> NamesforFile = new List<string>();
        XmlDocument docu = new XmlDocument();

        //Main Form
        Form1 Parent;
        #endregion

        public Training_Form(Form1 _Parent)
        {
            InitializeComponent();
            Parent = _Parent;
            Face = Parent.Face; //main form의 harrcascade
            //Face = new HaarCascade(Application.StartupPath + "/Cascades/haarcascade_frontalface_alt2.xml");
            ENC_Parameters.Param[0] = ENC;
            Image_Encoder_JPG = GetEncoder(ImageFormat.Jpeg);
            initialise_capture();
        }

        //train 정보 this.form -> main form
        private void Training_Form_FormClosing(object sender, FormClosingEventArgs e)
        {
            stop_capture();
            Parent.retrain();
            Parent.initialise_capture();
        }

        //Camera Start Stop
        public void initialise_capture()
        {
            grabber = new VideoCapture(0);
            grabber.FrameWidth = 640;
            grabber.FrameHeight = 480;
            //Initialize the FrameGraber event
            Application.Idle += new EventHandler(FrameGrabber);
        }
        private void stop_capture()
        {
            //Initialize the FrameGraber event
            Application.Idle -= new EventHandler(FrameGrabber);
            if (grabber != null)
            {
                grabber.Dispose();
            }
        }

        //Process Frame
        void FrameGrabber(object sender, EventArgs e)
        {
            //Get the current frame form capture device
            grabber.Read(currentFrame);

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

                    result = new Mat(currentFrame, facesDetected[i]);
                    Cv2.Resize(result, result, new OpenCvSharp.Size(100, 100), 0, 0, InterpolationFlags.Cubic);
                    Cv2.CvtColor(result, result, ColorConversionCodes.BGR2GRAY);
                    Cv2.EqualizeHist(result, result);
                    face_PICBX.ImageIpl = result;
                    //draw the face detected in the ith gray channel with red color
                    Cv2.Rectangle(currentFrame, facesDetected[i], Scalar.Red, 0);

                }
                if (RECORD && facesDetected.Length > 0 && resultImages.Count < num_faces_to_aquire)
                {
                    resultImages.Add(result);
                    count_lbl.Text = "Count: " + resultImages.Count.ToString();
                    if (resultImages.Count == num_faces_to_aquire)
                    {
                        ADD_BTN.Enabled = true;
                        NEXT_BTN.Visible = true;
                        PREV_btn.Visible = true;
                        count_lbl.Visible = false;
                        Single_btn.Visible = true;
                        ADD_ALL.Visible = true;
                        RECORD = false;
                        Application.Idle -= new EventHandler(FrameGrabber);
                    }
                }

                image_PICBX.ImageIpl = currentFrame;
            }
        }

        //Saving The Data
        private bool save_training_data(Mat face_data)
        {
            try
            {
                Random rand = new Random();
                bool file_create = true;
                string facename = "face_" + NAME_PERSON.Text + "_" + rand.Next().ToString() + ".jpg";
                while (file_create)
                {

                    if (!File.Exists(Application.StartupPath + @"/TrainedFaces/" + facename))
                    {
                        Console.WriteLine("file exists");
                        file_create = false;
                    }
                    else
                    {
                        Console.WriteLine("file doesn't exists");
                        facename = "face_" + NAME_PERSON.Text + "_" + rand.Next().ToString() + ".jpg";
                    }
                }


                if (Directory.Exists(Application.StartupPath + @"/TrainedFaces/"))
                {
                    Cv2.ImWrite(Application.StartupPath + @"/TrainedFaces/" + facename, face_data);
                    Console.WriteLine("file created");
                }
                else
                {
                    Directory.CreateDirectory(Application.StartupPath + @"/TrainedFaces/");
                    Cv2.ImWrite(Application.StartupPath + @"/TrainedFaces/" + facename, face_data);
                    Console.WriteLine("file and direcory created");
                }
                if (File.Exists(Application.StartupPath + @"/TrainedFaces/TrainedLabels.xml"))
                {
                    //File.AppendAllText(Application.StartupPath + "/TrainedFaces/TrainedLabels.txt", NAME_PERSON.Text + "\n\r");
                    bool loading = true;
                    while (loading)
                    {
                        try
                        {
                            Console.WriteLine("xml load");
                            docu.Load(Application.StartupPath + @"/TrainedFaces/TrainedLabels.xml");
                            loading = false;
                        }
                        catch
                        {
                            Console.WriteLine("xml doasn't exists");
                            docu = null;
                            docu = new XmlDocument();
                            Thread.Sleep(10);
                        }
                    }

                    //Get the root element
                    XmlElement root = docu.DocumentElement;

                    XmlElement face_D = docu.CreateElement("FACE");
                    XmlElement name_D = docu.CreateElement("NAME");
                    XmlElement file_D = docu.CreateElement("FILE");

                    //Add the values for each nodes
                    //name.Value = textBoxName.Text;
                    //age.InnerText = textBoxAge.Text;
                    //gender.InnerText = textBoxGender.Text;
                    name_D.InnerText = NAME_PERSON.Text;
                    file_D.InnerText = facename;

                    //Construct the Person element
                    //person.Attributes.Append(name);
                    face_D.AppendChild(name_D);
                    face_D.AppendChild(file_D);

                    //Add the New person element to the end of the root element
                    root.AppendChild(face_D);

                    //Save the document
                    Console.WriteLine("xml save");
                    docu.Save(Application.StartupPath + @"/TrainedFaces/TrainedLabels.xml");
                    //XmlElement child_element = docu.CreateElement("FACE");
                    //docu.AppendChild(child_element);
                    //docu.Save("TrainedLabels.xml");
                }
                else
                {
                    FileStream FS_Face = File.OpenWrite(Application.StartupPath + @"/TrainedFaces/TrainedLabels.xml");
                    Console.WriteLine("xml wrote");
                    using (XmlWriter writer = XmlWriter.Create(FS_Face))
                    {
                        writer.WriteStartDocument();
                        writer.WriteStartElement("Faces_For_Training");

                        writer.WriteStartElement("FACE");
                        writer.WriteElementString("NAME", NAME_PERSON.Text);
                        writer.WriteElementString("FILE", facename);
                        writer.WriteEndElement();

                        writer.WriteEndElement();
                        writer.WriteEndDocument();
                    }
                    FS_Face.Close();
                }

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                return false;
            }

        }
        private ImageCodecInfo GetEncoder(ImageFormat format)
        {
            ImageCodecInfo[] codecs = ImageCodecInfo.GetImageDecoders();
            foreach (ImageCodecInfo codec in codecs)
            {
                if (codec.FormatID == format.Guid)
                {
                    return codec;
                }
            }
            return null;
        }

        //Delete all the old training data by simply deleting the folder
        private void Delete_Data_BTN_Click(object sender, EventArgs e)
        {
            if (Directory.Exists(Application.StartupPath + @"/TrainedFaces/"))
            {
                Console.WriteLine("directory delete");
                Directory.Delete(Application.StartupPath + @"/TrainedFaces/", true);
                Directory.CreateDirectory(Application.StartupPath + @"/TrainedFaces/");
                Console.WriteLine("directory created");
            }
        }

        //Add the image to training data
        private void ADD_BTN_Click(object sender, EventArgs e)
        {
            if (resultImages.Count == num_faces_to_aquire)
            {
                Console.WriteLine("image full");
                if (!save_training_data(face_PICBX.ImageIpl)) MessageBox.Show("Error", "Error in saving file info. Training data not saved", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            else
            {
                Console.WriteLine("image not full");
                Bitmap img = OpenCvSharp.Extensions.BitmapConverter.ToBitmap(face_PICBX.ImageIpl);
                stop_capture();
                if (!save_training_data(face_PICBX.ImageIpl)) MessageBox.Show("Error", "Error in saving file info. Training data not saved", MessageBoxButtons.OK, MessageBoxIcon.Error);
                initialise_capture();
            }
        }
        private void Single_btn_Click(object sender, EventArgs e)
        {
            RECORD = false;
            resultImages.Clear();
            NEXT_BTN.Visible = false;
            PREV_btn.Visible = false;
            Application.Idle += new EventHandler(FrameGrabber);
            Single_btn.Visible = false;
            count_lbl.Text = "Count: 0";
            count_lbl.Visible = true;
        }
        //Get 10 image to train
        private void RECORD_BTN_Click(object sender, EventArgs e)
        {
            if (RECORD)
            {
                RECORD = false;
            }
            else
            {
                if (resultImages.Count == 10)
                {
                    resultImages.Clear();
                    Application.Idle += new EventHandler(FrameGrabber);
                    Console.WriteLine("image wrote success(10)");
                }
                RECORD = true;
                ADD_BTN.Enabled = false;
            }

        }
        private void NEXT_BTN_Click(object sender, EventArgs e)
        {
            if (results_list_pos < resultImages.Count - 1)
            {
                //face_PICBX.ImageIpl = OpenCvSharp.Extensions.BitmapConverter.ToMat(resultImages[results_list_pos]);
                face_PICBX.ImageIpl = resultImages[results_list_pos];
                results_list_pos++;
                Console.WriteLine("next btn");
                PREV_btn.Enabled = true;
            }
            else
            {
                NEXT_BTN.Enabled = false;
            }
        }
        private void PREV_btn_Click(object sender, EventArgs e)
        {
            if (results_list_pos > 0)
            {
                results_list_pos--;
                face_PICBX.ImageIpl = resultImages[results_list_pos];
                Console.WriteLine("prev btn");
                NEXT_BTN.Enabled = true;
            }
            else
            {
                PREV_btn.Enabled = false;
            }
        }
        private void ADD_ALL_Click(object sender, EventArgs e)
        {
            for (int i = 0; i < resultImages.Count; i++)
            {
                face_PICBX.ImageIpl = resultImages[i];
                //Bitmap img = OpenCvSharp.Extensions.BitmapConverter.ToBitmap(face_PICBX.ImageIpl);
                Console.WriteLine("add all!");
                if (!save_training_data(face_PICBX.ImageIpl)) MessageBox.Show("Error", "Error in saving file info. Training data not saved", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Thread.Sleep(100);
            }
            ADD_ALL.Visible = false;
            //restart single face detection
            Single_btn_Click(null, null);
        }

    }
}
