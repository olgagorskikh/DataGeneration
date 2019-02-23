using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Collections.Generic;
using Meta.Numerics.Statistics.Distributions;
using System.Configuration;
using System.Media;
using System.IO;
//using AForge;

namespace MainStatisticCalculator
{
    public partial class MainForm : Form
    {
        public MainForm()
        {
            InitializeComponent();
        }

        #region general variables

        //matrix of independent variables
        public static double[][] X = new double[n][];
        //matrix of dependent variables
        public static double[][] Y = new double[n][];
        //matrix of residuals
        public static double[][] eps = new double[n][];

        //6 rows - 2 rows per each y, i.e. 1 change in 1 y. Total 3 y-s
        public static double[,] SmallBettas = new double[6, 5];
        public static double[,] MediumBettas = new double[6, 5];
        public static double[,] LargeBettas = new double[6, 5];
        public static double[,] HugeBettas = new double[6, 5];
        //general Bettas variable
        public static double[,] Bettas = new double[6, 5];

        //distributional part
        public static Random rng = new Random();
        public static NormalDistribution XDistribution = new NormalDistribution(0, 1);
        public static UniformDistribution outliersProbability = new UniformDistribution(Meta.Numerics.Interval.FromEndpoints(0, 1));
        public static NormalDistribution outliersDistribution = new NormalDistribution(0,6);
        public static NormalDistribution EpsNormDistribution = new NormalDistribution(0,0.1);
        public static StudentDistribution EpsStudDistribution = new StudentDistribution(3);

        #endregion

        #region main settings

        //Changeable settings
        public static string[] distrs = { "norm", "stud3" };
        public static double[] outliers = { 0.00, 0.10 };
        public static string[] magnits = { "Small", "Medium", "Large", "Huge" };
        public const int iterations = 1000;
        public const int n = 600; // number of points
        public const int m = 5; // number of params
        public const int p = 3; //number of y-s

        //Change points
        private static int first = 60;
        private static int second = 300;
        private static int third = 480;

        //Folder to publish the results at
        private static string rootFolder = "C:\\Data";

        #endregion

        private void bGo_Click(object sender, EventArgs e)
        {
            //main estimation launch
            RunEstimation();
        }

        public void RunEstimation()
        {
            GetMemory();
            InitializeBettas();

            //length of a progress bar
            pbStatus.Maximum = magnits.Length * distrs.Length * outliers.Length * iterations;
            int a = 0;

            foreach (var magn in magnits)
            {
                foreach (var outlier in outliers)
                {
                    foreach (var distr in distrs)
                    {
                        for (int i = 0; i < iterations; i++)
                        {
                            //Generate one set for under particular settings
                            GenerateIndividualSet(distr, outlier, magn, (i + 1).ToString());
                            //increase progress bar value
                            a = a + 1;
                            pbStatus.Value = a;
                        }
                    }

                }
            }

            System.Media.SystemSounds.Question.Play();
            MessageBox.Show("Done!");

        }

        public static void GenerateIndividualSet(string distr, double outlier, string magn, string iteration)
        {
            
            string partPath = rootFolder + magn + "\\" + outlier.ToString() + "\\" + distr + "\\" + iteration;
            Directory.CreateDirectory(partPath);

            //Generate noise for all responses of the whole dataset
            for (var i = 0; i < n; i++)
                eps[i] = GenerateNoise(p, distr, outlier);

            for (var j = 0; j < n; j++)
                for (int i = 0; i < m; i++)
                    X[j][i] = XDistribution.GetRandomValue(rng);

            for (var j = 0; j < n; j++)
                for (var k = 0; k < p; k++)
                    Y[j][k] = 0;

            for (var i = 0; i < n; i++)
                CalculateCustomFunction(i, magn);

            
            
            PrintGeneratedData(partPath + "\\Data.txt");            
        }

        public static void CalculateCustomFunction(int j, string magn)
        {
            //Choose Bettas
            if (magn == "Small")
                Bettas = SmallBettas;
            else if (magn == "Medium")
                Bettas = MediumBettas;
            else if (magn == "Large")
                Bettas = LargeBettas;
            else
                Bettas = HugeBettas;

            for (int i = 0; i < m; i++)
            {
                //First y changes after point first
                if (j >= 0 && j < first)
                    Y[j][0] += X[j][i] * Bettas[0, i];
                
                if (j >= first)
                    Y[j][0] += X[j][i] * Bettas[1, i];

                //Second y changes after point second
                if (j >= 0 && j < second)
                    Y[j][1] += X[j][i] * Bettas[2, i];

                if (j >= second)
                    Y[j][1] += X[j][i] * Bettas[3, i];

                //Third y changes after point third
                if (j >= 0 && j < third)
                    Y[j][2] += X[j][i] * Bettas[4, i];

                if (j >= third)
                    Y[j][2] += X[j][i] * Bettas[5, i];

            }

            for (var i = 0; i < p; i++ )
                Y[j][i] += eps[j][i];


        }

      

        public static double[] GenerateNoise(int k, string distr, double outlier)
        {
            double[] result = new double[k];

            for (var i = 0; i < k; i++)
            {
                //the probability the point will be outlying
                var variation = outliersProbability.GetRandomValue(rng);

                //if a point will be outlying
                if (variation < outlier)
                    result[i] = outliersDistribution.GetRandomValue(rng);
                        
                //if a point will be regular
                else
                {
                        //depending on the original error distribution we generate a value
                        switch (distr)
                        {
                        case "norm":
                            result[i] = EpsNormDistribution.GetRandomValue(rng);
                            break;
                        case "stud3":
                            result[i] = EpsStudDistribution.GetRandomValue(rng);
                            break;
                        }
                }
            }                

            return result;
        }

        //we have assigned Bettas ONLY 
        //for max p = 5, 
        //for a model with three y-s each changing one time
        public static void InitializeBettas()
        {
            //Small magnitude d = 2
            for (var i = 0; i < 6; i++)
            {
                switch (i)
                {   
                    //Variable 1
                    case 0:
                        SmallBettas[i, 0] = 1; SmallBettas[i, 1] = 1; SmallBettas[i, 2] = 1; SmallBettas[i, 3] = 0; SmallBettas[i, 4] = 0;                       
                        break;
                    case 1:
                        SmallBettas[i, 0] = 3; SmallBettas[i, 1] = 1; SmallBettas[i, 2] = 1; SmallBettas[i, 3] = 0; SmallBettas[i, 4] = 0;                      
                        break;
                    //Variable 2
                    case 2:
                        SmallBettas[i, 0] = 1; SmallBettas[i, 1] = 3; SmallBettas[i, 2] = 1; SmallBettas[i, 3] = 0; SmallBettas[i, 4] = 0; 
                        break;
                    case 3:
                        SmallBettas[i, 0] = 1; SmallBettas[i, 1] = 5; SmallBettas[i, 2] = 1; SmallBettas[i, 3] = 0; SmallBettas[i, 4] = 0;
                        break;
                    //Variable 3
                    case 4:
                        SmallBettas[i, 0] = 1; SmallBettas[i, 1] = 1; SmallBettas[i, 2] = 3; SmallBettas[i, 3] = 0; SmallBettas[i, 4] = 0;
                        break;
                    case 5:
                        SmallBettas[i, 0] = 1; SmallBettas[i, 1] = 1; SmallBettas[i, 2] = 1; SmallBettas[i, 3] = 0; SmallBettas[i, 4] = 0;
                        break;
                }
            }

            //Medium magnitude d = 2
            for (var i = 0; i < 6; i++)
            {
                switch (i)
                {
                    //Variable 1
                    case 0:
                        MediumBettas[i, 0] = 1; MediumBettas[i, 1] = 1; MediumBettas[i, 2] = 1; MediumBettas[i, 3] = 0; MediumBettas[i, 4] = 0;
                        break;
                    case 1:
                        MediumBettas[i, 0] = 1; MediumBettas[i, 1] = 0; MediumBettas[i, 2] = 1; MediumBettas[i, 3] = 0; MediumBettas[i, 4] = 1;
                        break;
                    //Variable 2
                    case 2:
                        MediumBettas[i, 0] = 1; MediumBettas[i, 1] = 0; MediumBettas[i, 2] = 1; MediumBettas[i, 3] = 0; MediumBettas[i, 4] = 1; 
                        break;
                    case 3:
                        MediumBettas[i, 0] = 2; MediumBettas[i, 1] = 1; MediumBettas[i, 2] = 1; MediumBettas[i, 3] = 0; MediumBettas[i, 4] = 1;
                        break;
                    //Variable 3
                    case 4:
                        MediumBettas[i, 0] = 3; MediumBettas[i, 1] = 1; MediumBettas[i, 2] = 1; MediumBettas[i, 3] = 0; MediumBettas[i, 4] = 0;
                        break;
                    case 5:
                        MediumBettas[i, 0] = 3; MediumBettas[i, 1] = 1; MediumBettas[i, 2] = 0; MediumBettas[i, 3] = 1; MediumBettas[i, 4] = 0;
                        break;
                }
            }

            //Large magnitude d = 10
            for (var i = 0; i < 6; i++)
            {
                switch (i)
                {
                    //Variable 1
                    case 0:
                        LargeBettas[i, 0] = 1; LargeBettas[i, 1] = 1; LargeBettas[i, 2] = 1; LargeBettas[i, 3] = 0; LargeBettas[i, 4] = 0;
                        break;
                    case 1:
                        LargeBettas[i, 0] = 0; LargeBettas[i, 1] = 5; LargeBettas[i, 2] = 3; LargeBettas[i, 3] = 0; LargeBettas[i, 4] = 3;
                        break;
                    //Variable 2
                    case 2:
                        LargeBettas[i, 0] = 5; LargeBettas[i, 1] = 0; LargeBettas[i, 2] = 3; LargeBettas[i, 3] = 0; LargeBettas[i, 4] = 4;
                        break;
                    case 3:
                        LargeBettas[i, 0] = 5; LargeBettas[i, 1] = 0; LargeBettas[i, 2] = 0; LargeBettas[i, 3] = 6; LargeBettas[i, 4] = 5;
                        break;
                    //Variable 3
                    case 4:
                        LargeBettas[i, 0] = 0; LargeBettas[i, 1] = 1; LargeBettas[i, 2] = 1; LargeBettas[i, 3] = 0; LargeBettas[i, 4] = 0;
                        break;
                    case 5:
                        LargeBettas[i, 0] = 5; LargeBettas[i, 1] = 1; LargeBettas[i, 2] = 1; LargeBettas[i, 3] = 5; LargeBettas[i, 4] = 0;
                        break;
                }
            }

            //Huge magnitude d = 20
            for (var i = 0; i < 6; i++)
            {
                switch (i)
                {   
                    //Variable 1                 
                    case 0:
                        HugeBettas[i, 0] = 1; HugeBettas[i, 1] = 1; HugeBettas[i, 2] = 1; HugeBettas[i, 3] = 0; HugeBettas[i, 4] = 0;
                        break;
                    case 1:
                        HugeBettas[i, 0] = 11; HugeBettas[i, 1] = 0; HugeBettas[i, 2] = 7; HugeBettas[i, 3] = 0; HugeBettas[i, 4] = 3;
                        break;
                    //Variable 2
                    case 2:
                        HugeBettas[i, 0] = 11; HugeBettas[i, 1] = 10; HugeBettas[i, 2] = 0; HugeBettas[i, 3] = 7; HugeBettas[i, 4] = 0;
                        break;
                    case 3:
                        HugeBettas[i, 0] = 1; HugeBettas[i, 1] = 5; HugeBettas[i, 2] = 0; HugeBettas[i, 3] = 2; HugeBettas[i, 4] = 0;
                        break;
                    //Variable 3
                    case 4:
                        HugeBettas[i, 0] = 0; HugeBettas[i, 1] = 5; HugeBettas[i, 2] = 0; HugeBettas[i, 3] = 5; HugeBettas[i, 4] = 0;
                        break;
                    case 5:
                        HugeBettas[i, 0] = 5; HugeBettas[i, 1] = 0; HugeBettas[i, 2] = 5; HugeBettas[i, 3] = 0; HugeBettas[i, 4] = 0;
                        break;


                }
            }
        }


        #region Utility
        private static void GetMemory()
        {
            for (var i = 0; i < n; i++)
            {
                X[i] = new double[m];
                Y[i] = new double[p];
            }

            for (var i = 0; i < n; i++)
                eps[i] = new double[p];
        }

      
        //k - index of y
        private static void PrintGeneratedData(string path)
        {
            var text = string.Empty;

            for (var i = 0; i < n; i++)
            {
                for (var j = 0; j < p; j++ )
                    text += string.Format("{0:0.00}", Y[i][j]) + " ";

                text += ";";

                for (var j = 0; j < m; j++)
                    if (j != m - 1) text += string.Format("{0:0.00}", X[i][j]) + " ";
                    else text += string.Format("{0:0.00}", X[i][j]) + Environment.NewLine;
            }

            text = text.Replace(',', '.');
            System.IO.File.WriteAllText(path, text);
        }
  

        #endregion

       

    }
}
