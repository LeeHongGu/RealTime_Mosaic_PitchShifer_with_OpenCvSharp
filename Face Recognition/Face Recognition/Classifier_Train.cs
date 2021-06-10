using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using OpenCvSharp;
using OpenCvSharp.UserInterface;
using OpenCvSharp.Face;
using OpenCvSharp.Extensions;

using System.IO;
using System.Xml;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;
using System.Xml.Serialization;
using System.Drawing.Imaging;
using System.Drawing;

/*********************************************************************
 * 
 * https://courses.media.mit.edu/2010fall/mas622j/ProblemSets/slidesPCA.pdf 
 * https://towardsdatascience.com/face-recognition-how-lbph-works-90ec258c3d6b
 * https://blog.devgenius.io/face-recognition-based-on-lbph-algorithm-17acd65ca5f7
 * 위 자료들을 기반으로 Face recognization 공부 후 작성
 * 
 * PCA 기반
 * Eigen Face
 * https://darkpgmr.tistory.com/110 PCA에 대해 깊게 설명
 * PCA Algorithm 기반으로 작성한 코드 -> PCA.txt
 * 
 * LDA 기반
 * Fisher Face 
 * 
 * PCA : 고차원 공간에서 최대한 많은 분산을 유지하면서 차원 감소를 수행합니다.
 * LDA : 클래스 식별 정보를 최대한 보존하면서 차원 축소를 수행합니다.
 * 
 * -->SIFT SURF와 근접한 방식
 * 
 * LBPH : Local Binary Pattern Histogram, 
 * 각 픽셀의 이웃을 임계 값으로 지정하여 이미지의 픽셀에 레이블을 지정하고 결과를 이진수로 간주
 * https://towardsdatascience.com/face-recognition-how-lbph-works-90ec258c3d6b LBPH 설명
*********************************************************************/

/// <summary>
/// main form에서 EigenRecognizer code를 제거하는 방식으로 설계
/// </summary>
class Classifier_Train : IDisposable
{

    #region Variables

    //Eigen
    FaceRecognizer recognizer;

    //training variables
    List<Mat> trainingImages = new List<Mat>();//Images
    List<string> Names_List = new List<string>(); //labels
    List<int> Names_List_ID = new List<int>();
    int ContTrain, NumLabels;
    double Eigen_Distance = 0;
    string Eigen_label;
    int Eigen_threshold = 2000;

    //variables for process
    string Error;
    bool _IsTrained = false;

    public string Recognizer_Type = "OpenCvSharp.face.EigenFaceRecognizer";
    #endregion

    #region Constructors
    /// <summary>
    /// 기본 생성자, (Application.StartupPath + "\\TrainedFaces") for traing data.
    /// </summary>
    public Classifier_Train()
    {
        Console.WriteLine("classifier train");
        _IsTrained = LoadTrainingData(Application.StartupPath + @"\\TrainedFaces");
    }

    /// <summary>
    /// Takes String input to a different location for training data
    /// </summary>
    /// <param name="Training_Folder"></param>
    public Classifier_Train(string Training_Folder)
    {
        Console.WriteLine("classifier train(file)");
        _IsTrained = LoadTrainingData(Training_Folder);
    }
    #endregion

    #region Public
    /// <summary>
    /// no reset recognizer type, retrains the recognizer
    /// </summary>
    /// <returns></returns>
    public bool Retrain()
    {
        Console.WriteLine("retrain");
        return _IsTrained = LoadTrainingData(Application.StartupPath + @"\\TrainedFaces");
    }
    /// <summary>
    /// no reset recognizer type, retrains the recognizer
    /// Takes String input to a different location for training data.
    /// </summary>
    /// <returns></returns>
    public bool Retrain(string Training_Folder)
    {
        return _IsTrained = LoadTrainingData(Training_Folder);
    }

    /// <summary>
    /// <para>Return(True): If Training data and Trained Eigen Recogniser exist</para>
    /// <para>Return(False): If NO Training data and error in training has occured</para>
    /// </summary>
    public bool IsTrained
    {
        get { return _IsTrained; }
    }

    /// <summary>
    /// Recognise a Grayscale Image using the trained Eigen Recogniser
    /// </summary>
    /// <param name="Input_image"></param>
    /// <returns></returns>
    public string Recognise(Mat Input_image, int Eigen_Thresh = -1)
    {
        if (_IsTrained)
        {
            int predit = recognizer.Predict(Input_image);
            recognizer.Predict(Input_image, out predit, out Eigen_Distance);
            //EigenFaceRecognizer.

            if (predit == -1)
            {
                Eigen_label = "Unknown";
                Eigen_Distance = 0;
                return Eigen_label;
            }
            else
            {
                Eigen_label = Names_List[predit];
                //Eigen_Distance = (double)recognizer.get;
                if (Eigen_Thresh > -1) Eigen_threshold = Eigen_Thresh;

                //Only use the post threshold rule using Eigen Recognizer 
                //dont use during Fisher and LBHP threshold 
                switch (Recognizer_Type)
                {
                    case ("OpenCvSharp.face.EigenFaceRecognizer"):
                        if (Eigen_Distance > Eigen_threshold) return Eigen_label;
                        else return "Unknown";
                    case ("OpenCvSharp.face.LBPHFaceRecognizer"):
                    case ("OpenCvSharp.face..FisherFaceRecognizer"):
                    default:
                        return Eigen_label; //the threshold set in training controls unknowns
                }
            }

        }
        else return "";
    }

    /// <summary>
    /// Sets the threshold confidence value for string Recognise(Mat Input_image) to be used.
    /// </summary>
    public int Set_Eigen_Threshold
    {
        set
        {
            Eigen_threshold = value;
        }
    }

    /// <summary>
    /// Returns a string contaning the recognised persons name
    /// </summary>
    public string Get_Eigen_Label
    {
        get
        {
            return Eigen_label;
        }
    }

    /// <summary>
    /// Returns a Double confidence value for potential false classifications
    /// </summary>
    public double Get_Eigen_Distance
    {
        get
        {
            return Eigen_Distance;
        }
    }

    /// <summary>
    /// Returns a string contatining any error that has occured
    /// </summary>
    public string Get_Error
    {
        get { return Error; }
    }

    /// <summary>
    /// Saves the trained Eigen Recogniser
    /// </summary>
    /// <param name="filename"></param>
    public void Save_Eigen_Recogniser(string filename)
    {
        recognizer.Save(filename);

        //save label data
        string direct = Path.GetDirectoryName(filename);
        FileStream Label_Data = File.OpenWrite(direct + "/Labels.xml");
        using (XmlWriter writer = XmlWriter.Create(Label_Data))
        {
            writer.WriteStartDocument();
            writer.WriteStartElement("Labels_For_Recognizer_sequential");
            for (int i = 0; i < Names_List.Count; i++)
            {
                writer.WriteStartElement("LABEL");
                writer.WriteElementString("POS", i.ToString());
                writer.WriteElementString("NAME", Names_List[i]);
                writer.WriteEndElement();
            }

            writer.WriteEndElement();
            writer.WriteEndDocument();
        }
        Label_Data.Close();
    }

    /// <summary>
    /// Loads the trained Eigen Recogniser
    /// </summary>
    /// <param name="filename"></param>
    public void Load_Eigen_Recogniser(string filename)
    {
        //get the recogniser type from the file
        string ext = Path.GetExtension(filename);
        switch (ext)
        {
            case (".LBPH"):
                Recognizer_Type = "OpenCvSharp.face.LBPHFaceRecognizer";
                recognizer = LBPHFaceRecognizer.Create(1, 8, 8, 8, 100);//50
                break;
            case (".FFR"):
                Recognizer_Type = "OpenCvSharp.face.FisherFaceRecognizer";
                recognizer = FisherFaceRecognizer.Create(0, 3500);//4000
                break;
            case (".EFR"):
                Recognizer_Type = "OpenCvSharp.face.EigenFaceRecognizer";
                recognizer = EigenFaceRecognizer.Create(80, double.PositiveInfinity);
                break;
        }

        recognizer.Read(filename);

        //load the labels
        string direct = Path.GetDirectoryName(filename);
        Names_List.Clear();
        if (File.Exists(direct + "/Labels.xml"))
        {
            FileStream filestream = File.OpenRead(direct + "/Labels.xml");
            long filelength = filestream.Length;
            byte[] xmlBytes = new byte[filelength];
            filestream.Read(xmlBytes, 0, (int)filelength);
            filestream.Close();

            MemoryStream xmlStream = new MemoryStream(xmlBytes);

            using (XmlReader xmlreader = XmlTextReader.Create(xmlStream))
            {
                while (xmlreader.Read())
                {
                    if (xmlreader.IsStartElement())
                    {
                        switch (xmlreader.Name)
                        {
                            case "NAME":
                                if (xmlreader.Read())
                                {
                                    Names_List.Add(xmlreader.Value.Trim());
                                }
                                break;
                        }
                    }
                }
            }
            ContTrain = NumLabels;
        }
        _IsTrained = true;

    }

    /// <summary>
    /// Dispose of Class call Garbage Collector
    /// </summary>
    public void Dispose()
    {
        recognizer = null;
        trainingImages = null;
        Names_List = null;
        Error = null;
        GC.Collect();
    }

    #endregion

    #region Private
    /// <summary>
    /// Loads the traing data given folder location
    /// </summary>
    /// <param name="Folder_location"></param>
    /// <returns></returns>
    private bool LoadTrainingData(string Folder_location)
    {
        if (File.Exists(Folder_location + @"\\TrainedLabels.xml"))
        {
            try
            {
                //message_bar.Text = "";
                Names_List.Clear();
                Names_List_ID.Clear();
                trainingImages.Clear();
                FileStream filestream = File.OpenRead(Folder_location + @"\\TrainedLabels.xml");
                Console.WriteLine("Classifier xml file wrote");

                long filelength = filestream.Length;
                byte[] xmlBytes = new byte[filelength];
                filestream.Read(xmlBytes, 0, (int)filelength);
                filestream.Close();

                MemoryStream xmlStream = new MemoryStream(xmlBytes);

                using (XmlReader xmlreader = XmlTextReader.Create(xmlStream))
                {
                    while (xmlreader.Read())
                    {
                        if (xmlreader.IsStartElement())
                        {
                            switch (xmlreader.Name)
                            {
                                case "NAME":
                                    if (xmlreader.Read())
                                    {
                                        Names_List_ID.Add(Names_List.Count); //0, 1, 2, 3...
                                        Names_List.Add(xmlreader.Value.Trim());
                                        NumLabels += 1;
                                        Console.WriteLine("read name");
                                    }
                                    break;
                                case "FILE":
                                    if (xmlreader.Read())
                                    {
                                        Mat mat = Cv2.ImRead(Application.StartupPath + @"\\TrainedFaces\\" + xmlreader.Value.Trim(), ImreadModes.Grayscale);
                                        //trainingImages.Add(new Mat(Application.StartupPath + @"\\TrainedFaces\\" + xmlreader.Value.Trim()));
                                        trainingImages.Add(mat);
                                        Console.WriteLine("read file");
                                    }
                                    break;
                            }
                        }
                    }
                }
                ContTrain = NumLabels;

                if (trainingImages.ToArray().Length != 0)
                {
                    /*******************************
                    //Eigen face recognizer
                    //Eigen Uses
                    //          0 - X = unknown
                    //          > X = Recognised
                    //
                    //Fisher and LBPH Use
                    //          0 - X = Recognised
                    //          > X = Unknown
                    //
                    // Where X = Threshold value
                    *********************************/

                    switch (Recognizer_Type)
                    {
                        case ("OpenCvSharp.face.LBPHFaceRecognizer"):
                            Recognizer_Type = "OpenCvSharp.face.LBPHFaceRecognizer";
                            recognizer = LBPHFaceRecognizer.Create(1, 8, 8, 8, 100);//50
                            Console.WriteLine(Recognizer_Type);
                            break;
                        case ("OpenCvSharp.face.FisherFaceRecognizer"):
                            Recognizer_Type = "OpenCvSharp.face.FisherFaceRecognizer";
                            recognizer = FisherFaceRecognizer.Create(0, 3500);//4000
                            Console.WriteLine(Recognizer_Type);
                            break;
                        case ("OpenCvSharp.face.EigenFaceRecognizer"):
                            Recognizer_Type = "OpenCvSharp.face.EigenFaceRecognizer";
                            recognizer = EigenFaceRecognizer.Create(80, double.PositiveInfinity);
                            Console.WriteLine(Recognizer_Type);
                            break;
                    }

                    //List<Mat> mats = new List<Mat>();

                    //for (int i = 0; i < trainingImages.Count; i++)
                    //{
                    //    // mats.Add(OpenCvSharp.Extensions.BitmapConverter.ToMat(trainingImages[i]));
                    //    if (!trainingImages[i].IsContinuous())
                    //        trainingImages[i] = trainingImages[i].Clone();
                    //}
                    //아직 opencv에서 안고침...ㅂㄷㅂㄷ

                    recognizer.Train(trainingImages.ToArray(), Names_List_ID.ToArray());
                    Console.WriteLine("recog train");

                    return true;
                }
                else return false;
            }
            catch (Exception ex)
            {
                Error = ex.ToString();
                Console.WriteLine(Error);
                return false;
            }
        }
        else return false;
    }

    #endregion
}

