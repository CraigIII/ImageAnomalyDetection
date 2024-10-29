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

        //�p�ⲧ�`�{�ɭȪ��禡
        double CalculateAnomalyThreshold(List<float[]> features)
        {
            double totalDistance = 0;
            int count = features.Count;

            foreach (var feature in features)
            {
                totalDistance += CalculateDistanceToCenter(feature, features);      //�[�`�Z�������I���Z��
            }
            return totalDistance / count * 1.5;                                                                 //�H�����Z����1.5�����{�ɭ�
        }

        //�p��Ϥ����S�x�V�q�Z�������I���Z��
        double CalculateDistanceToCenter(float[] feature, List<float[]> features)
        {
            int length = feature.Length;
            var centroid = new float[length];
            foreach (var f in features)                             // �[�`�Ϥ��S�x�V�q�����e��
            {
                for (int i = 0; i < length; i++)
                    centroid[i] += f[i];
            }

            for (int i = 0; i < length; i++)                        // �p��Ϥ��S�x�V�q��������
                centroid[i] /= features.Count;

            double distance = 0;                                        //�p��öǦ^�Ϥ���Euclidean distance(�ڴX���w�Z��)
            for (int i = 0; i < length; i++)
                distance += Math.Pow(feature[i] - centroid[i], 2);

            return Math.Sqrt(distance);
        }

        // ���J�V�m�Ϥ���
        IEnumerable<ImageData> LoadImageData(string folder)
        {
            foreach (var file in Directory.GetFiles(folder, "*.jpg"))               // Ū����m�V�m�Ϥ�����Ƨ����Ҧ���JPG�Ϥ�
            {
                yield return new ImageData { data = file };                                 // �N�Ϥ�����ƫإߦ�ImageData���O������
            }
        }

        private void btnTrain_Click(object sender, EventArgs e)
        {
            string imagesFolder = "images";                                                                   //  ���w��m�V�m�Ϥ�����Ƨ��W��
            string modelPath = "PreTrainedModel/resnet50-v2-7.onnx";            // ���w���ϥΪ��w�V�m�ҫ�
            var mlContext = new MLContext();                                                              // �إ�MLContext���O������

            var imageData = LoadImageData(imagesFolder).ToArray();              // ���J�V�m�Ϥ�
            var data = mlContext.Data.LoadFromEnumerable(imageData);        // �ǳƦ��V�m���

            var pipeline = mlContext.Transforms.LoadImages("data", "", nameof(ImageData.data))  // �N�Ϥ��ǳƦ��w�V�m�ҫ����w���j�p
                .Append(mlContext.Transforms.ResizeImages("data", 224, 224))
                .Append(mlContext.Transforms.ExtractPixels("data"))
                .Append(mlContext.Transforms.ApplyOnnxModel(
                    modelFile: modelPath,
                    outputColumnNames: new[] { "resnetv24_dense0_fwd" },
                    inputColumnNames: new[] { "data" }));

            var model = pipeline.Fit(data);                                                 // ����V�m: �ϥΫ��w���w�V�m�ҫ�����Ϥ����S�x�V�q
            var transformedData = model.Transform(data);                // �p��C�@�i�V�m�Ϥ����S�x�V�q

            // �N�V�m�Ϥ����S�x�V�q�ǳƦ�List���X
            var featuresList = new List<float[]>();
            foreach (var features in mlContext.Data.CreateEnumerable<ImageFeatures>(transformedData, reuseRowObject: false))
            {
                featuresList.Add(features.Features);
            }

            var threshold = CalculateAnomalyThreshold(featuresList);        //�p�ⲧ�`���{�ɭ�

            int i = 0;
            foreach (var feature in featuresList)                                                   // �ХܯS�x�j���{�ɭȪ��Ϥ������`
            {
                var distance = CalculateDistanceToCenter(feature, featuresList);
                bool isAnomaly = distance > threshold;
                Trace.WriteLine($"Image: {imageData[i++].data}, Anomaly: {isAnomaly}, Distance: {distance}");
            }
        }
    }
}
