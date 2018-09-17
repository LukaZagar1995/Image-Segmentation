using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace WindowsFormsApp1
{
    public partial class Form1 : Form
    {
        int imageHeight, imageWidth, rows;
        string[,] dataset;
        string filePath = String.Empty;
        static string[,] datasetTranformed;
        Bitmap transformedImage;

        public class StringTable
        {
            public string[] ColumnNames { get; set; }
            public string[,] Values { get; set; }
        }

        public Form1()
        {
           
            InitializeComponent();
            comboBox1.Items.Add("Euclidean");
            comboBox1.Items.Add("Cosine"); //Iz nekog razloga se nemože koristiti parametar udaljenosti
            comboBox1.SelectedIndex = 0; //Odabir početnog elementa(parametra) padajućeg izbornika
            button3.Visible = false ;

        }

        private void button1_Click(object sender, EventArgs e)
        {

           
            OpenFileDialog of = new OpenFileDialog();
            of.Filter = "Image Files (*.bmp;*.jpg;*.jpeg,*.png)|*.BMP;*.JPG;*.JPEG;*.PNG";
            if (of.ShowDialog() == DialogResult.OK)
            {
                filePath = of.FileName;
                pictureBox1.ImageLocation = of.FileName;

               
            }

            Bitmap image = new Bitmap(of.FileName);
            transformedImage = new Bitmap(image);
            imageHeight = image.Height;
            imageWidth = image.Width;

            rows = imageHeight * imageWidth;
            int row = 0;
            dataset = new string[rows, 6]; 
            for (int i = 0; i < imageWidth; i++)
            {
                for (int j = 0; j < imageHeight; j++)
                {
                    //Priprema podataka
                    Color pixel = image.GetPixel(i, j);
                    dataset[row, 0] = i.ToString();
                    dataset[row, 1] = j.ToString();
                    dataset[row, 2] = pixel.R.ToString();
                    dataset[row, 3] = pixel.G.ToString();
                    dataset[row, 4] = pixel.B.ToString();
                    dataset[row, 5] = pixel.A.ToString();
                    row++;

                }
            }


        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }

        private void button2_Click(object sender, EventArgs e)
        {
            //Upravljanje greškama
            if (filePath == String.Empty)
            {
                MessageBox.Show("Unesite sliku!", "Upozorenje!",
                            MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if (brojCentara.Value <2)
            {
                MessageBox.Show("Broj centara mora biti veći od 2!", "Upozorenje!",
                            MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if (brojIteracija.Value < 1)
            {
                MessageBox.Show("Broj iteracija mora biti veći od 1!", "Upozorenje!",
                            MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if (comboBox1.SelectedItem == null)
            {
                MessageBox.Show("Odaberite mjeru udaljenosti", "Upozorenje!",
                            MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            
            int numberOfCentroids = Convert.ToInt32(Math.Round(brojCentara.Value, 0));
            int iterations = Convert.ToInt32(Math.Round(brojIteracija.Value, 0));

            string Metric = comboBox1.SelectedItem.ToString();

            InvokeRequestResponseService(iterations, numberOfCentroids, Metric, dataset, rows).Wait();

            if (datasetTranformed != null)
            {
                int x, y, R, G, B, A;
                for (int i = 0; i < rows; i++)
                {
                    //Učitavanje obrađene slike
                    x = Int32.Parse(dataset[i, 0]);
                    y = Int32.Parse(dataset[i, 1]);
                    R = (int)double.Parse(datasetTranformed[i, 0], CultureInfo.InvariantCulture.NumberFormat);
                    G = (int)double.Parse(datasetTranformed[i, 1], CultureInfo.InvariantCulture.NumberFormat);
                    B = (int)double.Parse(datasetTranformed[i, 2], CultureInfo.InvariantCulture.NumberFormat);
                    A = Int32.Parse(dataset[i, 5]);
                    transformedImage.SetPixel(x, y, Color.FromArgb(A, R, G, B));

                }

                pictureBox3.Image = transformedImage;
            }

        }

        private void numericUpDown1_ValueChanged(object sender, EventArgs e)
        {

        }

        private void label1_Click(object sender, EventArgs e)
        {

        }

        private void label2_Click(object sender, EventArgs e)
        {

        }

        private void button3_Click(object sender, EventArgs e)
        {
            Form2 f2 = new Form2();
            f2.ShowDialog();
        }

        private void pictureBox3_Click(object sender, EventArgs e)
        {

        }

        private void pictureBox1_Click(object sender, EventArgs e)
        {

        }
        static async Task InvokeRequestResponseService(int iterations, int numberOfCentroids, string Metric, string [,] dataset, int rows)
        {
            using (var client = new HttpClient())
            {
                var scoreRequest = new
                {

                    Inputs = new Dictionary<string, StringTable>() {
                        {
                            "input1",
                            new StringTable()
                            {
                                ColumnNames = new string[] {"X", "Y", "R", "G", "B", "A"},
                                Values = dataset //
                            }
                        },
                    },
                    GlobalParameters = new Dictionary<string, string>() {
        { "Iterations", iterations.ToString() },
        { "Number of Centroids", numberOfCentroids.ToString() },
        { "Metric", Metric },
}
                };
                const string apiKey = "5Klufp2YzyjtFcjsuHzjQV1MN1MgHQvnJ47wBrrT9PHhYh9HyKminKPgeDX0LqgRpEun7iAYImMhFBfH7Rk4dA=="; //API ključ
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

                client.BaseAddress = new Uri("https://ussouthcentral.services.azureml.net/workspaces/7b7d186476b445d6b6f680c5686dbc81/services/36d41008942644b5a6b0cbd7e112d6a3/execute?api-version=2.0&details=true");

                HttpResponseMessage response = await client.PostAsJsonAsync("", scoreRequest).ConfigureAwait(false);

                 if (response.IsSuccessStatusCode)
                {
                    string[] resultArray;
                    datasetTranformed = new string[rows,3];
                    int j = 0, x= 0, counter = 0;
                    Boolean readyToWrite = false;
                    string RGBstring, partString = String.Empty;
                    string result = await response.Content.ReadAsStringAsync(); //Odgovor web servisa
                    resultArray = result.Split(':');//Parsiranje odgovora web servisa
                    RGBstring = resultArray[7].Replace("}", "");
                    for (int i = 1; i < RGBstring.Length - 1; i++)
                    {
                        if (RGBstring[i] == '[')
                        {
                            while (RGBstring[i] != ']')
                            {
                                if (RGBstring[i] == '"')
                                {
                                    i++;
                                    counter++;
                                    if (counter % 2 == 0)
                                    {
                                        readyToWrite = true;
                                    }
                                }
                                if (counter % 2 != 0)
                                {
                                    partString = partString + RGBstring[i];
                                }
                                if(readyToWrite)
                                {
                                    datasetTranformed[x,j] = partString; //Spremanje vrijednosti u 2D polje
                                    j++;
                                    partString = "";
                                    readyToWrite = false;
                                }

                                if (counter == 6)
                                {
                                    j = 0;
                                    break;
                                }

                                i++;

                            }
                            x++;
                            counter = 0;
                                
                        }
                    } 

                }
                else
                {
                    Console.WriteLine(string.Format("The request failed with status code: {0}", response.StatusCode));

                    // Print the headers - they include the requert ID and the timestamp, which are useful for debugging the failure
                    Console.WriteLine(response.Headers.ToString());

                    string responseContent = await response.Content.ReadAsStringAsync();
                    Console.WriteLine(responseContent);
                }
            }
        }
    }
}
