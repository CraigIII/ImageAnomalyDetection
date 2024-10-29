using Microsoft.ML.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImageAnomalyDetection
{
    // 描述訓練圖片特徵向量的類別
    public class ImageFeatures 
    {
        [ColumnName("resnetv24_dense0_fwd")]           // 指定輸出特徵向量的名稱(必須符合所使用的圖片辨識模型搭配)
        public float[] Features { get; set; }                           // 記載訓練圖片特徵向量的屬性
    }
}
