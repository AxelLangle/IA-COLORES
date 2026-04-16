#nullable disable
#pragma warning disable CS8618 
#pragma warning disable CS8622 

using System;
using System.Drawing;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Collections.Generic; // Agregado para usar List<string>
using IAupt.uptRNA;

namespace PML_COLORES
{
    public partial class Form1 : Form
    {
        private Button btnEntrenar;
        private Button btnCargarImagen;
        private Button btnVerRueda;
        private Button btnCopiarLog;
        private NumericUpDown numBrillo;
        private TrackBar tbBrillo;
        private PictureBox picVisor;
        private Panel panelMuestraColor;
        private RichTextBox txtConsola;
        private SplitContainer splitPrincipal;

        private bool mostrandoRueda = true;
        private Bitmap imagenSubida = null;

        private System.Windows.Forms.Timer timerEntrenamiento;
        private System.Diagnostics.Stopwatch cronometro;

        private readonly string[] nombresColores = {
            "Rojo", "Verde", "Azul",
            "Amarillo", "Cian", "Magenta",
            "Naranja", "Morado", "Rosa",
            "Blanco", "Negro", "Gris"
        };

        public Form1()
        {
            GenerarDatasetColoresInteligente();
            ConfigurarInterfaz();
        }

        private void ConfigurarInterfaz()
        {
            this.Text = "Clasificador Cromático IA - Perceptrón Multicapa";
            this.Size = new Size(1200, 700);
            this.MinimumSize = new Size(900, 550);
            this.BackColor = Color.FromArgb(250, 250, 250);
            this.StartPosition = FormStartPosition.CenterScreen;

            splitPrincipal = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Vertical,
                SplitterWidth = 6,
                BackColor = Color.FromArgb(220, 220, 220)
            };

            this.Shown += (s, e) => { AjustarPaneles(); ActualizarVisor(); };
            this.Resize += (s, e) => { AjustarPaneles(); ActualizarVisor(); };

            Panel panelIzq = splitPrincipal.Panel1;
            panelIzq.BackColor = Color.FromArgb(250, 250, 250);
            Panel panelDer = splitPrincipal.Panel2;
            panelDer.BackColor = Color.FromArgb(230, 230, 230);

            Label lblTitulo = new Label { Text = "Sensor de Color IA", Font = new Font("Segoe UI Semibold", 16F), ForeColor = Color.FromArgb(30, 30, 30), Location = new Point(15, 15), AutoSize = true };

            btnEntrenar = new Button { Text = "1. Entrenar Cerebro", Font = new Font("Segoe UI", 10F, FontStyle.Bold), BackColor = Color.FromArgb(0, 120, 215), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Location = new Point(20, 60), Size = new Size(splitPrincipal.Panel1.Width - 40, 40), Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right, Cursor = Cursors.Hand };
            btnEntrenar.FlatAppearance.BorderSize = 0;
            btnEntrenar.Click += BtnEntrenar_Click;

            timerEntrenamiento = new System.Windows.Forms.Timer();
            timerEntrenamiento.Interval = 100;
            timerEntrenamiento.Tick += (s, e) =>
            {
                if (cronometro != null && cronometro.IsRunning)
                {
                    btnEntrenar.Text = $"⏳ Entrenando... {cronometro.Elapsed.TotalSeconds:F1}s";
                }
            };

            btnVerRueda = new Button { Text = "🔴 Círculo Cromático", Font = new Font("Segoe UI", 9F), BackColor = Color.FromArgb(230, 230, 230), ForeColor = Color.Black, FlatStyle = FlatStyle.Flat, Location = new Point(20, 110), Size = new Size((splitPrincipal.Panel1.Width - 50) / 2, 35), Anchor = AnchorStyles.Top | AnchorStyles.Left, Cursor = Cursors.Hand };
            btnVerRueda.FlatAppearance.BorderSize = 0;
            btnVerRueda.Click += (s, e) => { mostrandoRueda = true; ActualizarVisor(); };

            btnCargarImagen = new Button { Text = "🖼️ Subir Imagen", Font = new Font("Segoe UI", 9F), BackColor = Color.FromArgb(230, 230, 230), ForeColor = Color.Black, FlatStyle = FlatStyle.Flat, Location = new Point(25 + btnVerRueda.Width, 110), Size = new Size((splitPrincipal.Panel1.Width - 50) / 2, 35), Anchor = AnchorStyles.Top | AnchorStyles.Right, Cursor = Cursors.Hand };
            btnCargarImagen.FlatAppearance.BorderSize = 0;
            btnCargarImagen.Click += BtnCargarImagen_Click;

            Label lblBrillo = new Label { Text = "Luminosidad del Círculo:", Font = new Font("Segoe UI", 9F, FontStyle.Bold), Location = new Point(20, 160), AutoSize = true };
            numBrillo = new NumericUpDown { Location = new Point(splitPrincipal.Panel1.Width - 80, 158), Size = new Size(60, 25), Minimum = 10, Maximum = 100, Value = 100, Font = new Font("Segoe UI", 9F), Anchor = AnchorStyles.Top | AnchorStyles.Right };
            tbBrillo = new TrackBar { Location = new Point(15, 185), Size = new Size(splitPrincipal.Panel1.Width - 30, 45), Minimum = 10, Maximum = 100, Value = 100, TickStyle = TickStyle.None, Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right };

            tbBrillo.ValueChanged += (s, e) => { numBrillo.Value = tbBrillo.Value; if (mostrandoRueda) ActualizarVisor(); };
            numBrillo.ValueChanged += (s, e) => { tbBrillo.Value = (int)numBrillo.Value; };

            panelMuestraColor = new Panel { Location = new Point(20, 235), Size = new Size(splitPrincipal.Panel1.Width - 40, 50), BackColor = Color.White, BorderStyle = BorderStyle.FixedSingle, Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right };

            btnCopiarLog = new Button { Text = "📋 Copiar Logs", Font = new Font("Segoe UI", 9F, FontStyle.Bold), BackColor = Color.FromArgb(210, 210, 210), ForeColor = Color.FromArgb(50, 50, 50), FlatStyle = FlatStyle.Flat, Location = new Point(20, 300), Size = new Size(130, 30), Cursor = Cursors.Hand };
            btnCopiarLog.FlatAppearance.BorderSize = 0;
            btnCopiarLog.Click += BtnCopiarLog_Click;

            txtConsola = new RichTextBox
            {
                Location = new Point(20, 340),
                Size = new Size(splitPrincipal.Panel1.Width - 40, splitPrincipal.Panel1.Height - 360),
                Font = new Font("Consolas", 10F),
                BackColor = Color.FromArgb(245, 245, 245),
                BorderStyle = BorderStyle.None,
                ReadOnly = true,
                Text = "Da clic en 'Entrenar' y toca la imagen/círculo.\n",
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right
            };

            panelIzq.Controls.Add(lblTitulo);
            panelIzq.Controls.Add(btnEntrenar);
            panelIzq.Controls.Add(btnVerRueda);
            panelIzq.Controls.Add(btnCargarImagen);
            panelIzq.Controls.Add(lblBrillo);
            panelIzq.Controls.Add(numBrillo);
            panelIzq.Controls.Add(tbBrillo);
            panelIzq.Controls.Add(panelMuestraColor);
            panelIzq.Controls.Add(btnCopiarLog);
            panelIzq.Controls.Add(txtConsola);

            picVisor = new PictureBox { Dock = DockStyle.Fill, SizeMode = PictureBoxSizeMode.Zoom, Cursor = Cursors.Cross };
            picVisor.MouseClick += PicVisor_MouseClick;
            panelDer.Controls.Add(picVisor);

            this.Controls.Add(splitPrincipal);
        }

        private void AjustarPaneles()
        {
            if (this.Width > 0 && this.WindowState != FormWindowState.Minimized)
            {
                try
                {
                    int distanciaIdeal = this.Width / 3;
                    int minimo = splitPrincipal.Panel1MinSize;
                    int maximo = splitPrincipal.Width - splitPrincipal.Panel2MinSize;

                    // Forzamos matemáticamente a que la distancia se mantenga en el rango legal
                    if (maximo >= minimo)
                    {
                        splitPrincipal.SplitterDistance = Math.Max(minimo, Math.Min(maximo, distanciaIdeal));
                    }
                }
                catch { /* Silenciamos cualquier conflicto gráfico durante repintados abruptos */ }
            }
        }
        private void Log(string msj) { txtConsola.AppendText($"> {msj}\n"); txtConsola.ScrollToCaret(); }

        private void BtnCopiarLog_Click(object sender, EventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(txtConsola.Text)) { Clipboard.SetText(txtConsola.Text); MessageBox.Show("Log copiado."); }
        }

        private void GenerarDatasetColoresInteligente()
        {
            System.Globalization.CultureInfo formatoPunto = System.Globalization.CultureInfo.InvariantCulture;
            List<string> entradas = new List<string>();
            List<string> salidas = new List<string>();

            // ¡NUEVO! Función ayudante que nos facilita inyectar colores y su respuesta correcta
            void AgregarPatron(double inR, double inG, double inB, int claseObjetivo)
            {
                // Seguridad: Limitamos los valores estrictamente entre 0.0 y 1.0
                inR = Math.Max(0, Math.Min(1, inR));
                inG = Math.Max(0, Math.Min(1, inG));
                inB = Math.Max(0, Math.Min(1, inB));

                entradas.Add($"{inR.ToString("F3", formatoPunto)}\t{inG.ToString("F3", formatoPunto)}\t{inB.ToString("F3", formatoPunto)}");

                string outStr = "";
                for (int s = 0; s < 12; s++) { outStr += (s == claseObjetivo) ? "1\t" : "0\t"; }
                salidas.Add(outStr.Trim());
            }

            double[,] coloresBase = {
        { 1.0, 0.0, 0.0 },     // 0. Rojo
        { 0.0, 1.0, 0.0 },     // 1. Verde
        { 0.0, 0.0, 1.0 },     // 2. Azul
        { 1.0, 1.0, 0.0 },     // 3. Amarillo
        { 0.0, 1.0, 1.0 },     // 4. Cian
        { 1.0, 0.0, 1.0 },     // 5. Magenta
        { 1.0, 0.5, 0.0 },     // 6. Naranja
        { 0.5, 0.0, 0.5 },     // 7. Morado
        { 1.0, 0.75, 0.8 },    // 8. Rosa
        { 1.0, 1.0, 1.0 },     // 9. Blanco
        { 0.0, 0.0, 0.0 },     // 10. Negro
        { 0.5, 0.5, 0.5 }      // 11. Gris
    };

            // 1. Entrenar los 9 Colores Vivos (Clases 0 a 8)
            for (int c = 0; c < 9; c++)
            {
                double r = coloresBase[c, 0];
                double g = coloresBase[c, 1];
                double b = coloresBase[c, 2];

                AgregarPatron(r, g, b, c); // Color puro

                // Sombras ricas (de 90% a 30% de luz)
                for (double intensidad = 0.9; intensidad >= 0.3; intensidad -= 0.1)
                    AgregarPatron(r * intensidad, g * intensidad, b * intensidad, c);

                // Brillos / Tonos Pastel (mezcla con blanco)
                for (double mezcla = 0.1; mezcla <= 0.8; mezcla += 0.1)
                    AgregarPatron(r + (1.0 - r) * mezcla, g + (1.0 - g) * mezcla, b + (1.0 - b) * mezcla, c);
            }

            // 2. Entrenar BLANCO a prueba de balas (Clase 9)
            AgregarPatron(1.0, 1.0, 1.0, 9);
            for (double v = 0.85; v <= 0.99; v += 0.02)
            {
                AgregarPatron(v, v, v, 9);
                AgregarPatron(v, v + 0.01, v - 0.02, 9); // Blanco sucio/ruidoso
            }

            // 3. Entrenar NEGRO a prueba de balas (Clase 10)
            AgregarPatron(0.0, 0.0, 0.0, 10);
            for (double v = 0.02; v <= 0.20; v += 0.02) // Todos los valores bajísimos (como el 23,20,26)
            {
                AgregarPatron(v, v, v, 10); // Negro grisáceo uniforme
                AgregarPatron(v + 0.03, v - 0.01, v + 0.02, 10); // Negro rojizo/azulado
                AgregarPatron(v - 0.02, v + 0.04, v - 0.01, 10); // Negro verdoso
            }

            // 4. Entrenar GRIS a prueba de balas (Clase 11)
            AgregarPatron(0.5, 0.5, 0.5, 11);
            for (double v = 0.25; v <= 0.75; v += 0.05)
            {
                AgregarPatron(v, v, v, 11); // Gris perfecto
                AgregarPatron(v + 0.04, v - 0.03, v + 0.01, 11); // Gris con contaminación de color
                AgregarPatron(v - 0.02, v + 0.03, v - 0.02, 11); // Gris con contaminación de color
            }

            // Finalmente, escribimos el archivo. El entradas.Count calculará automáticamente los +350 patrones.
            using (StreamWriter file = new StreamWriter("cerebro_crom.pml"))
            {
                file.WriteLine("3 3 20 12");
                file.WriteLine("0.1");
                file.WriteLine("30000");
                file.WriteLine("0.01");
                file.WriteLine(entradas.Count.ToString());
                file.WriteLine("");

                foreach (var e in entradas) file.WriteLine(e);
                file.WriteLine("");
                foreach (var s in salidas) file.WriteLine(s);
            }
        }

        private async void BtnEntrenar_Click(object sender, EventArgs e)
        {
            try
            {
                btnEntrenar.Enabled = false;
                btnEntrenar.BackColor = Color.FromArgb(80, 80, 80);
                Log("Iniciando entrenamiento en segundo plano...");

                cronometro = System.Diagnostics.Stopwatch.StartNew();
                timerEntrenamiento.Start();

                PerceptronMultiCapa rna = null;

                await Task.Run(() =>
                {
                    rna = new PerceptronMultiCapa(@"cerebro_crom.pml");
                    rna.entrenar();
                });

                cronometro.Stop();
                timerEntrenamiento.Stop();

                btnEntrenar.Text = "1. Entrenar Cerebro";
                btnEntrenar.BackColor = Color.FromArgb(0, 120, 215);
                btnEntrenar.Enabled = true;

                Log($"¡Entrenamiento exitoso! Tiempo: {cronometro.Elapsed.TotalSeconds:F2}s | Iteraciones: {rna.iteracionesAlcanzadas} | Error: {rna.E:F4}");
            }
            catch (Exception ex)
            {
                cronometro?.Stop();
                timerEntrenamiento?.Stop();
                btnEntrenar.Text = "1. Entrenar Cerebro";
                btnEntrenar.BackColor = Color.FromArgb(0, 120, 215);
                btnEntrenar.Enabled = true;
                Log("ERROR al entrenar: " + ex.Message);
            }
        }

        private void BtnCargarImagen_Click(object sender, EventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog { Filter = "Imágenes|*.bmp;*.jpg;*.png|Todas|*.*" };
            if (ofd.ShowDialog() == DialogResult.OK)
            {
                imagenSubida = new Bitmap(ofd.FileName);
                mostrandoRueda = false;
                ActualizarVisor();
                Log("Imagen cargada. Haz clic en cualquier parte para analizar el color.");
            }
        }

        private void ActualizarVisor()
        {
            if (picVisor.Width <= 0 || picVisor.Height <= 0) return;

            if (mostrandoRueda)
            {
                int size = Math.Min(picVisor.Width, picVisor.Height) - 40;
                if (size <= 10) return;

                Bitmap rueda = new Bitmap(size, size);
                int radio = size / 2;
                double luminosidad = tbBrillo.Value / 100.0;

                for (int y = 0; y < size; y++)
                {
                    for (int x = 0; x < size; x++)
                    {
                        double dx = x - radio;
                        double dy = y - radio;
                        double distancia = Math.Sqrt(dx * dx + dy * dy);

                        if (distancia <= radio)
                        {
                            double angulo = Math.Atan2(dy, dx) * 180 / Math.PI;
                            if (angulo < 0) angulo += 360;
                            rueda.SetPixel(x, y, HsvARgb(angulo, distancia / radio, luminosidad));
                        }
                        else
                        {
                            rueda.SetPixel(x, y, Color.Transparent);
                        }
                    }
                }

                Bitmap fondo = new Bitmap(picVisor.Width, picVisor.Height);
                using (Graphics g = Graphics.FromImage(fondo))
                {
                    g.Clear(Color.FromArgb(230, 230, 230));
                    g.DrawImage(rueda, (picVisor.Width - size) / 2, (picVisor.Height - size) / 2);
                }

                picVisor.Image = fondo;
            }
            else
            {
                picVisor.Image = imagenSubida;
            }
        }

        private void PicVisor_MouseClick(object sender, MouseEventArgs e)
        {
            try
            {
                if (!File.Exists("cerebro_crom.ppm"))
                {
                    MessageBox.Show("Primero debes entrenar la red.", "Aviso");
                    return;
                }

                Bitmap bmp = new Bitmap(1, 1);
                using (Graphics g = Graphics.FromImage(bmp))
                {
                    g.CopyFromScreen(Cursor.Position, new Point(0, 0), new Size(1, 1));
                }
                Color c = bmp.GetPixel(0, 0);

                if (c.R == 230 && c.G == 230 && c.B == 230 && mostrandoRueda) return;

                panelMuestraColor.BackColor = c;
                string hexCode = $"#{c.R:X2}{c.G:X2}{c.B:X2}";

                Log("\n==================================");
                Log($"🎨 Análisis: RGB ({c.R}, {c.G}, {c.B}) | HEX {hexCode}");

                PerceptronMultiCapa rna = new PerceptronMultiCapa(@"cerebro_crom.ppm");

                double[] entradasRgb = { c.R / 255.0, c.G / 255.0, c.B / 255.0 };
                rna.reconocer(entradasRgb);

                double maxValor = -999;
                int indiceGanador = -1;

                int filas = rna.y.GetLength(0);

                for (int i = 0; i < 12; i++)
                {
                    double valorActual = (filas == 1) ? rna.y[0, i] : rna.y[i, 0];
                    if (valorActual > maxValor)
                    {
                        maxValor = valorActual;
                        indiceGanador = i;
                    }
                }

                string nombrePredictivo = (indiceGanador >= 0 && indiceGanador < 12) ? nombresColores[indiceGanador] : "Indefinido";

                Log($"🧠 Predicción IA: ¡Es {nombrePredictivo.ToUpper()}!");
                Log("==================================\n");
            }
            catch (Exception ex) { Log("Error en predicción: " + ex.Message); }
        }

        private Color HsvARgb(double h, double s, double v)
        {
            int hi = Convert.ToInt32(Math.Floor(h / 60)) % 6;
            double f = h / 60 - Math.Floor(h / 60);
            v = v * 255;
            int vInt = Convert.ToInt32(v);
            int p = Convert.ToInt32(v * (1 - s));
            int q = Convert.ToInt32(v * (1 - f * s));
            int t = Convert.ToInt32(v * (1 - (1 - f) * s));

            if (hi == 0) return Color.FromArgb(255, vInt, t, p);
            else if (hi == 1) return Color.FromArgb(255, q, vInt, p);
            else if (hi == 2) return Color.FromArgb(255, p, vInt, t);
            else if (hi == 3) return Color.FromArgb(255, p, q, vInt);
            else if (hi == 4) return Color.FromArgb(255, t, p, vInt);
            else return Color.FromArgb(255, vInt, p, q);
        }
    }
}