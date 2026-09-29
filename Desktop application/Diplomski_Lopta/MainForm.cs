using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows.Forms;
using Newtonsoft.Json;

namespace Diplomski_Lopta
{
    public class IgracInfo
    {
        public string Ime { get; set; }
        public string Broj { get; set; }
        public string Ppg { get; set; }
        public string Visina { get; set; }
        public string Godiste { get; set; }

        public IgracInfo(string ime, string broj, string ppg, string visina, string godiste)
        {
            Ime = ime;
            Broj = broj;
            Ppg = ppg;
            Visina = visina;
            Godiste = godiste;
        }
    }

    public partial class MainForm : Form
    {
        private static readonly HttpClient client = new HttpClient();
        private const string ApiUrl = "http://127.0.0.1:5000/istorija";

        private DataGridView gridView;
        private System.Windows.Forms.Timer updateTimer;
        private Button btnStartStop;

        private Panel pnlHeader;
        private Panel pnlPanel3;
        private Panel pnlPanel4;
        private Panel pnlPanel5;
        private Panel pnlPanel6;
        private Panel pnlPanel7;
        private Panel pnlPanel8;
        private Panel pnlPanel9;
        private Panel pnlCenterWrapper9;

        private ComboBox cmbTeam1;
        private ComboBox cmbTeam2;
        private FlowLayoutPanel flowPanelTeam1;
        private FlowLayoutPanel flowPanelTeam2;

        private readonly List<Udarac> listaUdaracaSesije = new List<Udarac>();
        private readonly HashSet<int> obradjeniRawId = new HashSet<int>();
        private bool isInitialFetch = true;

        private readonly Dictionary<string, List<IgracInfo>> timoviBaza =
            new Dictionary<string, List<IgracInfo>>();

        private readonly Color colFormBg = Color.FromArgb(244, 241, 234);
        private readonly Color colCardBg = Color.White;
        private readonly Color colHeaderBg = Color.FromArgb(232, 226, 214);
        private readonly Color colTextPrimary = Color.FromArgb(35, 38, 45);
        private readonly Color colTextSecondary = Color.FromArgb(90, 95, 105);
        private readonly Color colAccent = Color.FromArgb(212, 75, 42);
        private readonly Color colGridLine = Color.FromArgb(210, 205, 195);

        public MainForm()
        {
            InitializeComponent();
            PostaviIkonuProzora();
            InicijalizujBazuIgraca();
            BuildCustomUI();
        }

        private void PostaviIkonuProzora()
        {
            try
            {
                if (File.Exists("kosarkaska_lopta.ico"))
                {
                    this.Icon = new Icon("kosarkaska_lopta.ico");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Ikona nije učitana: " + ex.Message);
            }
        }

        private void InicijalizujBazuIgraca()
        {
            timoviBaza["Denver Nuggets"] = new List<IgracInfo>
            {
                new IgracInfo("Nikola Jokić", "15", "26.4 PPG", "211 cm", "1995."),
                new IgracInfo("Jamal Murray", "27", "21.2 PPG", "193 cm", "1997."),
                new IgracInfo("Aaron Gordon", "50", "13.9 PPG", "203 cm", "1995."),
                new IgracInfo("Michael Porter Jr.", "01", "16.7 PPG", "208 cm", "1998."),
                new IgracInfo("K. Caldwell-Pope", "05", "10.1 PPG", "196 cm", "1993."),
                new IgracInfo("Reggie Jackson", "07", "10.2 PPG", "188 cm", "1990."),
                new IgracInfo("Peyton Watson", "08", "6.7 PPG", "203 cm", "2002."),
                new IgracInfo("Christian Braun", "00", "7.3 PPG", "198 cm", "2001."),
                new IgracInfo("DeAndre Jordan", "06", "3.9 PPG", "211 cm", "1988."),
                new IgracInfo("Zeke Nnaji", "22", "3.2 PPG", "206 cm", "2001."),
                new IgracInfo("Julian Strawther", "03", "4.5 PPG", "201 cm", "2002."),
                new IgracInfo("Vlatko Čančar", "31", "5.0 PPG", "203 cm", "1997.")
            };

            timoviBaza["Partizan"] = new List<IgracInfo>
            {
                new IgracInfo("Carlik Jones", "2", "15.0 PPG", "1.85 cm", "1997."),
                new IgracInfo("Nikola Tanasković", "10", "12.1 PPG", "2.04 cm", "1997."),
                new IgracInfo("Aleksa Avramović", "33", "10.2 PPG", "192 cm", "1994."),
                new IgracInfo("Vanja Marinković", "21", "4.9 PPG", "199 cm", "1997."),
                new IgracInfo("Tonye Jekiri", "23", "9.2 PPG", "213 cm", "1994."),
                new IgracInfo("Arijan Lakić", "19", "8.9 PPG", "198 cm", "2000."),
                new IgracInfo("Mario Nakić", "07", "5.5 PPG", "202 cm", "2001."),
                new IgracInfo("Mitar Bošnjaković", "08", "4.0 PPG", "201 cm", "2006."),
                new IgracInfo("Alessandro Pajola", "06", "4.3 PPG", "194 cm", "1999."),
                new IgracInfo("Hayes Kevarrius", "19", "8.8 PPG", "206 cm", "1997."),
                new IgracInfo("Derek Willis", "45", "2.8 PPG", "206 cm", "1995."),
                new IgracInfo("Lamar Stevens", "00", "0 PPG", "201 cm", "1997.")
            };

            timoviBaza["Crvena Zvezda"] = new List<IgracInfo>
            {
                new IgracInfo("Chris Jones", "1", "12.3 PPG", "188 cm", "1993."),
                new IgracInfo("Nemanja Nedović", "26", "14.3 PPG", "191 cm", "1991."),
                new IgracInfo("Joel Bolomboy", "21", "10.2 PPG", "206 cm", "1994."),
                new IgracInfo("Nikola Kalinić", "07", "11.1 PPG", "202 cm", "1991."),
                new IgracInfo("Ebuka Izundu", "15", "12.0 PPG", "208 cm", "1996."),
                new IgracInfo("Stafan Miljenović", "02", "4.5 PPG", "193 cm", "2001."),
                new IgracInfo("Jordan Nwora", "33", "19.4 PPG", "2.04 cm", "1998."),
                new IgracInfo("David Kramer", "09", "4.1 PPG", "196 cm", "1997."),
                new IgracInfo("Semi Ojeleye", "37", "9.7 PPG", "198 cm", "1994."),
                new IgracInfo("Ognjen Dobrić", "13", "8.3 PPG", "200 cm", "1994."),
                new IgracInfo("Dejan Davidovac", "07", "6.2 PPG", "203 cm", "1995."),
                new IgracInfo("Nikola Đurišić", "11", "5.8 PPG", "2.03 cm", "2004.")
            };

            timoviBaza["Dallas Mavericks"] = new List<IgracInfo>
            {
                new IgracInfo("Luka Dončić", "77", "33.9 PPG", "201 cm", "1999."),
                new IgracInfo("Kyrie Irving", "11", "25.6 PPG", "188 cm", "1992."),
                new IgracInfo("Dereck Lively II", "02", "8.8 PPG", "216 cm", "2004."),
                new IgracInfo("Daniel Gafford", "21", "11.2 PPG", "208 cm", "1998."),
                new IgracInfo("PJ Washington", "25", "11.7 PPG", "201 cm", "1998."),
                new IgracInfo("Tim Hardaway Jr.", "10", "14.4 PPG", "196 cm", "1992."),
                new IgracInfo("Maxi Kleber", "42", "4.4 PPG", "208 cm", "1992."),
                new IgracInfo("Dante Exum", "00", "7.8 PPG", "196 cm", "1995."),
                new IgracInfo("Josh Green", "08", "8.2 PPG", "196 cm", "2000."),
                new IgracInfo("Derrick Jones Jr.", "55", "8.6 PPG", "198 cm", "1997."),
                new IgracInfo("Jaden Hardy", "01", "7.3 PPG", "193 cm", "2002."),
                new IgracInfo("Dwight Powell", "07", "3.3 PPG", "208 cm", "1991.")
            };
        }

        private void BuildCustomUI()
        {
            this.Size = new Size(1420, 900);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.Text = "Basketball Analytics System - Detection of last touch";
            this.BackColor = colFormBg;

            pnlHeader = new Panel
            {
                Dock = DockStyle.Top,
                Height = 65,
                BackColor = colCardBg,
                Padding = new Padding(10)
            };

            PictureBox picLogo = new PictureBox
            {
                Size = new Size(45, 45),
                Location = new Point(15, 10),
                SizeMode = PictureBoxSizeMode.Zoom
            };
            if (File.Exists("kosarkaska_lopta.ico"))
            {
                picLogo.Image = new Icon("kosarkaska_lopta.ico", 48, 48).ToBitmap();
            }

            Label lblTitle = new Label
            {
                Text = "PRIKAZ KONTAKATA SA LOPTOM U REALNOM VREMENU",
                Font = new Font("Segoe UI", 13, FontStyle.Bold),
                ForeColor = colTextPrimary,
                AutoSize = false,
                Size = new Size(650, 45),
                Location = new Point(70, 10),
                TextAlign = ContentAlignment.MiddleLeft
            };

            btnStartStop = new Button
            {
                Text = "⏸ STOP",
                Width = 120,
                Height = 40,
                Location = new Point(pnlHeader.Width - 140, 12),
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                BackColor = colAccent,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnStartStop.FlatAppearance.BorderSize = 0;

            btnStartStop.Click += (s, e) =>
            {
                if (updateTimer.Enabled)
                {
                    updateTimer.Stop();
                    btnStartStop.Text = "▶ START";
                    btnStartStop.BackColor = Color.FromArgb(40, 167, 69);
                }
                else
                {
                    updateTimer.Start();
                    btnStartStop.Text = "⏸ STOP";
                    btnStartStop.BackColor = colAccent;
                }
            };

            pnlHeader.Controls.Add(picLogo);
            pnlHeader.Controls.Add(lblTitle);
            pnlHeader.Controls.Add(btnStartStop);

            pnlPanel9 = PodrzavaneLige();

            Panel pnlCenterContainer = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(10),
                BackColor = colFormBg
            };

            pnlPanel3 = new Panel
            {
                Width = 640,
                Dock = DockStyle.Left,
                BackColor = colCardBg,
                Padding = new Padding(8)
            };

            pnlPanel4 = new Panel
            {
                Dock = DockStyle.Top,
                Height = 40,
                BackColor = colHeaderBg
            };

            Label lblIcon = new Label
            {
                Text = "📊",
                Font = new Font("Segoe UI Emoji", 11),
                ForeColor = colAccent,
                Location = new Point(10, 9),
                AutoSize = true
            };

            Label lblUdarciUzivo = new Label
            {
                Text = "Kontakti uživo",
                Font = new Font("Segoe UI", 11, FontStyle.Bold),
                ForeColor = colTextPrimary,
                Location = new Point(38, 8),
                AutoSize = true
            };

            pnlPanel4.Controls.Add(lblIcon);
            pnlPanel4.Controls.Add(lblUdarciUzivo);

            gridView = new DataGridView();
            Tabela();

            pnlPanel3.Controls.Add(gridView);
            pnlPanel3.Controls.Add(pnlPanel4);

            Panel pnlRightColumn = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(10, 0, 0, 0),
                BackColor = colFormBg
            };

            Panel pnlTeam1Block = new Panel
            {
                Dock = DockStyle.Top,
                Height = 350,
                BackColor = colCardBg,
                Padding = new Padding(8)
            };

            pnlPanel6 = new Panel
            {
                Dock = DockStyle.Top,
                Height = 42,
                BackColor = colHeaderBg
            };

            Label lblTeam1Tag = new Label
            {
                Text = "Izaberi Tim 1:",
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                ForeColor = colTextPrimary,
                Location = new Point(10, 10),
                AutoSize = true
            };

            cmbTeam1 = new ComboBox
            {
                Location = new Point(115, 7),
                Width = 220,
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 10),
                BackColor = Color.White,
                ForeColor = colTextPrimary
            };
            foreach (var t in timoviBaza.Keys) cmbTeam1.Items.Add(t);
            cmbTeam1.SelectedIndex = 1; // Partizan
            cmbTeam1.SelectedIndexChanged += (s, e) => Igraci(flowPanelTeam1, cmbTeam1.SelectedItem.ToString());

            pnlPanel6.Controls.Add(lblTeam1Tag);
            pnlPanel6.Controls.Add(cmbTeam1);

            flowPanelTeam1 = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                BackColor = colFormBg,
                Padding = new Padding(5)
            };

            pnlTeam1Block.Controls.Add(flowPanelTeam1);
            pnlTeam1Block.Controls.Add(pnlPanel6);

            Panel pnlTeam2Block = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = colCardBg,
                Margin = new Padding(0, 10, 0, 0),
                Padding = new Padding(8)
            };

            pnlPanel8 = new Panel
            {
                Dock = DockStyle.Top,
                Height = 42,
                BackColor = colHeaderBg
            };

            Label lblTeam2Tag = new Label
            {
                Text = "Izaberi Tim 2:",
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                ForeColor = colTextPrimary,
                Location = new Point(10, 10),
                AutoSize = true
            };

            cmbTeam2 = new ComboBox
            {
                Location = new Point(115, 7),
                Width = 220,
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 10),
                BackColor = Color.White,
                ForeColor = colTextPrimary
            };
            foreach (var t in timoviBaza.Keys) cmbTeam2.Items.Add(t);
            cmbTeam2.SelectedIndex = 2; // Crvena Zvezda
            cmbTeam2.SelectedIndexChanged += (s, e) => Igraci(flowPanelTeam2, cmbTeam2.SelectedItem.ToString());

            pnlPanel8.Controls.Add(lblTeam2Tag);
            pnlPanel8.Controls.Add(cmbTeam2);

            flowPanelTeam2 = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                BackColor = colFormBg,
                Padding = new Padding(5)
            };

            pnlTeam2Block.Controls.Add(flowPanelTeam2);
            pnlTeam2Block.Controls.Add(pnlPanel8);

            pnlRightColumn.Controls.Add(pnlTeam2Block);
            pnlRightColumn.Controls.Add(pnlTeam1Block);

            pnlCenterContainer.Controls.Add(pnlRightColumn);
            pnlCenterContainer.Controls.Add(pnlPanel3);

            this.Controls.Add(pnlCenterContainer);
            this.Controls.Add(pnlPanel9);
            this.Controls.Add(pnlHeader);

            Igraci(flowPanelTeam1, cmbTeam1.SelectedItem.ToString());
            Igraci(flowPanelTeam2, cmbTeam2.SelectedItem.ToString());

            updateTimer = new System.Windows.Forms.Timer { Interval = 1000 };
            updateTimer.Tick += async (s, e) => await UcitajPodatke();
            updateTimer.Start();

            _ = UcitajPodatke();
        }

        private Panel PodrzavaneLige()
        {
            Panel pnl9 = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 85,
                BackColor = colCardBg
            };

            pnlCenterWrapper9 = new Panel
            {
                Size = new Size(560, 55),
                BackColor = Color.Transparent
            };

            Label lblSupportedLeagues = new Label
            {
                Text = "SUPPORTED LEAGUES",
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                ForeColor = colTextPrimary,
                AutoSize = true,
                Location = new Point(10, 18)
            };

            Panel pnlLogos = new Panel
            {
                Size = new Size(360, 50),
                Location = new Point(190, 2)
            };

            TableLayoutPanel logoGrid = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 3,
                RowCount = 1
            };
            logoGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33f));
            logoGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33f));
            logoGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33f));

            logoGrid.Controls.Add(Logo("nba.png"), 0, 0);
            logoGrid.Controls.Add(Logo("aba.png"), 1, 0);
            logoGrid.Controls.Add(Logo("euroleague.png"), 2, 0);

            pnlLogos.Controls.Add(logoGrid);
            pnlCenterWrapper9.Controls.Add(lblSupportedLeagues);
            pnlCenterWrapper9.Controls.Add(pnlLogos);

            pnl9.Controls.Add(pnlCenterWrapper9);

            pnl9.Resize += (s, e) =>
            {
                pnlCenterWrapper9.Location = new Point(
                    (pnl9.Width - pnlCenterWrapper9.Width) / 2,
                    (pnl9.Height - pnlCenterWrapper9.Height) / 2
                );
            };

            return pnl9;
        }

        private void Igraci(FlowLayoutPanel container, string imeTima)
        {
            container.Controls.Clear();
            if (timoviBaza.ContainsKey(imeTima))
            {
                var igraci = timoviBaza[imeTima];
                foreach (var igrac in igraci)
                {
                    container.Controls.Add(PanelIgraca(igrac));
                }
            }
        }

        private Panel PanelIgraca(IgracInfo igrac)
        {
            Panel card = new Panel
            {
                Width = 180,
                Height = 88,
                BackColor = colCardBg,
                Margin = new Padding(5)
            };

            Label lblIme = new Label
            {
                Text = igrac.Ime,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                ForeColor = colAccent,
                Location = new Point(8, 6),
                AutoSize = true
            };

            Label lblStats = new Label
            {
                Text = $"#{igrac.Broj}  |  {igrac.Ppg}\nVisina: {igrac.Visina}\nGodište: {igrac.Godiste}",
                Font = new Font("Segoe UI", 8.5f),
                ForeColor = colTextPrimary,
                Location = new Point(8, 28),
                AutoSize = true
            };

            card.Controls.Add(lblIme);
            card.Controls.Add(lblStats);
            return card;
        }

        private PictureBox Logo(string filename)
        {
            PictureBox pb = new PictureBox
            {
                Dock = DockStyle.Fill,
                SizeMode = PictureBoxSizeMode.Zoom,
                Margin = new Padding(3)
            };

            try
            {
                if (File.Exists(filename))
                {
                    pb.Image = Image.FromFile(filename);
                }
            }
            catch { }

            return pb;
        }

        private void Tabela()
        {
            gridView.Dock = DockStyle.Fill;
            gridView.AutoGenerateColumns = true;
            gridView.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            gridView.RowHeadersVisible = false;

            gridView.BackgroundColor = colCardBg;
            gridView.BorderStyle = BorderStyle.None;

            gridView.CellBorderStyle = DataGridViewCellBorderStyle.Single;
            gridView.GridColor = colGridLine;

            gridView.EnableHeadersVisualStyles = false;
            gridView.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.Single;
            gridView.ColumnHeadersDefaultCellStyle.BackColor = colAccent;
            gridView.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            gridView.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9.5f, FontStyle.Bold);
            gridView.ColumnHeadersHeight = 36;

            gridView.DefaultCellStyle.BackColor = colCardBg;
            gridView.DefaultCellStyle.ForeColor = colTextPrimary;
            gridView.DefaultCellStyle.Font = new Font("Segoe UI", 9);
            gridView.DefaultCellStyle.SelectionBackColor = Color.FromArgb(235, 215, 200);
            gridView.DefaultCellStyle.SelectionForeColor = colTextPrimary;
            gridView.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        }

        private async Task UcitajPodatke()
        {
            try
            {
                HttpResponseMessage response = await client.GetAsync(ApiUrl);
                if (response.IsSuccessStatusCode)
                {
                    string json = await response.Content.ReadAsStringAsync();
                    var noviUdarciSaServera = JsonConvert.DeserializeObject<List<Udarac>>(json);

                    if (noviUdarciSaServera != null && noviUdarciSaServera.Count > 0 && gridView != null)
                    {
                        if (isInitialFetch)
                        {
                            foreach (var u in noviUdarciSaServera)
                            {
                                obradjeniRawId.Add(u.Id);
                            }

                            noviUdarciSaServera.Sort((a, b) => a.Id.CompareTo(b.Id));

                            int brojZaUcitavanje = 5;
                            var poslednjihPet = noviUdarciSaServera.GetRange(
                                Math.Max(0, noviUdarciSaServera.Count - brojZaUcitavanje),
                                Math.Min(brojZaUcitavanje, noviUdarciSaServera.Count)
                            );

                            foreach (var u in poslednjihPet)
                            {
                                u.Id = listaUdaracaSesije.Count + 1;
                                listaUdaracaSesije.Insert(0, u);
                            }

                            isInitialFetch = false;

                            gridView.DataSource = null;
                            gridView.DataSource = new List<Udarac>(listaUdaracaSesije);
                            return;
                        }

                        noviUdarciSaServera.Sort((a, b) => a.Id.CompareTo(b.Id));

                        bool imaNovih = false;

                        foreach (var u in noviUdarciSaServera)
                        {
                            if (!obradjeniRawId.Contains(u.Id))
                            {
                                obradjeniRawId.Add(u.Id);
                                u.Id = listaUdaracaSesije.Count + 1;
                                listaUdaracaSesije.Insert(0, u);
                                imaNovih = true;
                            }
                        }

                        if (imaNovih)
                        {
                            gridView.DataSource = null;
                            gridView.DataSource = new List<Udarac>(listaUdaracaSesije);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Greška sa serverom: " + ex.Message);
            }
        }
    }

    public class Udarac
    {
        [JsonProperty("id")]
        [DisplayName("Broj Udarca")]
        public int Id { get; set; }

        [JsonProperty("timestamp")]
        [DisplayName("Vreme Udarca")]
        public string Timestamp { get; set; }

        [JsonProperty("igrac")]
        [DisplayName("Igrač")]
        public string Igrac { get; set; }

        [JsonProperty("magnituda")]
        [DisplayName("Magnituda (N)")]
        public int Magnituda { get; set; }

        [JsonProperty("udaljenost")]
        [DisplayName("Udaljenost (m)")]
        public float Udaljenost { get; set; }
    }
}