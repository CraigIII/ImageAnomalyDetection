using Microsoft.ML;
using Microsoft.ML.Transforms.Image;
using Microsoft.ML.Transforms.Onnx;
using System.Data;
using System.Diagnostics;
using System.Windows.Forms;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace ImageAnomalyDetection
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        //計算異常臨界值的函式
        double CalculateAnomalyThreshold(List<float[]> features)
        {
            double totalDistance = 0;
            int count = features.Count;

            foreach (var feature in features)
            {
                totalDistance += CalculateDistanceToCenter(feature, features);      //加總距離中心點的距離
            }
            return totalDistance / count * 1.5;                                                                 //以平均距離的1.5倍為臨界值
        }

        //計算圖片的特徵向量距離中心點的距離
        double CalculateDistanceToCenter(float[] feature, List<float[]> features)
        {
            int length = feature.Length;
            var centroid = new float[length];
            foreach (var f in features)                             // 加總圖片特徵向量的內容值
            {
                for (int i = 0; i < length; i++)
                    centroid[i] += f[i];
            }

            for (int i = 0; i < length; i++)                        // 計算圖片特徵向量的平均值
                centroid[i] /= features.Count;

            double distance = 0;                                        //計算並傳回圖片的Euclidean distance(歐幾里德距離)
            for (int i = 0; i < length; i++)
                distance += Math.Pow(feature[i] - centroid[i], 2);

            return Math.Sqrt(distance);
        }

        // 載入訓練圖片集
        IEnumerable<ImageData> LoadImageData(string folder)
        {
            foreach (var file in Directory.GetFiles(folder, "*.jpg"))               // 讀取放置訓練圖片的資料夾中所有的JPG圖片
            {
                yield return new ImageData { data = file };                                 // 將圖片的資料建立成ImageData類別的物件
            }
        }

        private void btnTrain_Click(object sender, EventArgs e)
        {
            string imagesFolder = "images";                                                                   //  指定放置訓練圖片的資料夾名稱
            string modelPath = "PreTrainedModel/resnet50-v2-7.onnx";            // 指定欲使用的預訓練模型
            var mlContext = new MLContext();                                                              // 建立MLContext類別的物件

            var imageData = LoadImageData(imagesFolder).ToArray();              // 載入訓練圖片
            var data = mlContext.Data.LoadFromEnumerable(imageData);        // 準備成訓練資料

            var pipeline = mlContext.Transforms.LoadImages("data", "", nameof(ImageData.data))  // 將圖片準備成預訓練模型指定的大小
                .Append(mlContext.Transforms.ResizeImages("data", 224, 224))
                .Append(mlContext.Transforms.ExtractPixels("data"))
                .Append(mlContext.Transforms.ApplyOnnxModel(
                    modelFile: modelPath,
                    outputColumnNames: new[] { "resnetv24_dense0_fwd" },
                    inputColumnNames: new[] { "data" }));

            var model = pipeline.Fit(data);                                                 // 執行訓練: 使用指定的預訓練模型抽取圖片的特徵向量
            var transformedData = model.Transform(data);                // 計算每一張訓練圖片的特徵向量

            // 將訓練圖片的特徵向量準備成List集合
            var featuresList = new List<float[]>();
            foreach (var features in mlContext.Data.CreateEnumerable<ImageFeatures>(transformedData, reuseRowObject: false))
            {
                featuresList.Add(features.Features);
            }

            var threshold = CalculateAnomalyThreshold(featuresList);        //計算異常的臨界值

            int i = 0;
            foreach (var feature in featuresList)                                                   // 標示特徵大於臨界值的圖片為異常
            {
                var distance = CalculateDistanceToCenter(feature, featuresList);
                bool isAnomaly = distance > threshold;
                Trace.WriteLine($"Image: {imageData[i++].data}, Anomaly: {isAnomaly}, Distance: {distance}");
            }
        }
    }
}
