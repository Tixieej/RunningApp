using System;
using System.Linq;
using System.Collections.Generic;
using Android.Views;
using Android.App;
using Android.Widget;
using Android.OS;
using Android.Content;
using Android.Graphics;   // vanwege PointF
using Android.Locations;  // vanwege Location
using Android.Hardware;
using Android.Util;

namespace Plaatje
{
	[Activity(Label = "Plaatje", MainLauncher = true)]
	public class MainActivity : Activity,  ILocationListener
	{
		Scherm tekening;
		Button start, clear, center;
		Location huidig;
		LocationManager locMgr;


		string Provider;

		protected override void OnCreate(Bundle b)
		{
			base.OnCreate(b);

			initGps();

			tekening = new Scherm(this);
			start = new Button(this);
			clear = new Button(this);
			center = new Button(this);

			start.Text = "Start";
			start.SetBackgroundColor(Color.OliveDrab);
			start.Click += this.begin;

			clear.Text = "Clear";
			clear.SetBackgroundColor(Color.Tomato);
			clear.Click += this.weg;

			center.Text = "Center";
			center.SetBackgroundColor(Color.SkyBlue);
			center.Click += this.plek;


			LinearLayout.LayoutParams par;
			par = new LinearLayout.LayoutParams(LinearLayout.LayoutParams.MatchParent, LinearLayout.LayoutParams.WrapContent, 1f);
			LinearLayout stapel = new LinearLayout(this);
			LinearLayout rij = new LinearLayout(this);
			stapel.Orientation = Orientation.Vertical;
			rij.Orientation = Orientation.Horizontal;
			stapel.AddView(rij);
			rij.AddView(start, par);
			rij.AddView(clear, par);
			rij.AddView(center, par);
			stapel.AddView(tekening, par);


			this.SetContentView(stapel);
		}

		public void initGps()
		{
			locMgr = (LocationManager)GetSystemService(LocationService);
			Criteria criteriaForLocationService = new Criteria
			{
				Accuracy = Accuracy.Fine
			};
			IList<string> acceptableLocationProviders = locMgr.GetProviders(criteriaForLocationService, true);

			if (acceptableLocationProviders.Any())
			{
				Provider = acceptableLocationProviders.First();
				locMgr.RequestLocationUpdates(Provider, 2000, 1, this);
			}
			else {
				Provider = string.Empty;
				//foutmelding als GPS uit staat
				AlertDialog.Builder alert = new AlertDialog.Builder(this);
				alert.SetTitle("Zet aub uw GPS aan");
				Dialog dialog = alert.Create();
				dialog.Show();
			}

		}

		public void begin(object o, EventArgs ea)
		{
			if (!this.tekening.lijstbijhouden)
			{
				//als er op start gedrukt wordt: vraag huidige locatie op en stop in lijst
				//vraag dan om de zoveel meters of sec opnieuw locatie op en stop in lijst
				this.tekening.lijstbijhouden = true;
				start.Text = "Stop";
				this.tekening.route.Add(this.tekening.Stip);
			}
			else
			{
				this.tekening.lijstbijhouden = false;
				start.Text = "Start";
			}
			this.tekening.invalidateScherm();

		}

		public void weg(object o, EventArgs ea)
		{
			AlertDialog.Builder alert = new AlertDialog.Builder(this);
			alert.SetTitle("Weet u zeker dat u de route wilt verwijderen?");
			alert.SetNegativeButton("nee", NietWeg);
			alert.SetPositiveButton("ja", WelWeg);
			alert.Show();
		}

		protected void NietWeg(object o, EventArgs ea)
		{
			
		}
		protected void WelWeg(object o, EventArgs ea)
		{
			//maak de route leeg, hij verdwijnt van de kaart
			this.tekening.lijstbijhouden = false;
			this.tekening.route.Clear();
			this.tekening.invalidateScherm();
		}

		public void plek(object o, EventArgs ea)
		{
			//De huidige locatie wordt gecentreerd op het scherm.
			this.tekening.KaartPositie = new PointF((this.tekening.huidig.X + this.tekening.Utrecht.Width / 2), (this.tekening.huidig.Y + this.tekening.Utrecht.Height / 2));
			this.tekening.Stip = new PointF(this.tekening.Width/2, this.tekening.Height/2);
			this.tekening.invalidateScherm();
		}

		public void OnLocationChanged(Location loc)
		{
			tekening.locationChanged(loc);
		}

		public void OnProviderDisabled(string provider)
		{
		}
		public void OnProviderEnabled(string provider)
		{
		}

		public void OnStatusChanged(string provider, Availability status, Bundle extras)
		{
		}

		protected override void OnResume()
		{
			base.OnResume();
			initGps();
		}

		protected override void OnPause()
		{
			base.OnPause();
			locMgr.RemoveUpdates(this);
		}
	}

	public class Scherm : View
	{
		public Bitmap Utrecht;
		public float Schaal, Hoek;
		public PointF v1, v2, s1, s2, KaartPositie, sleepbegin, kaartbegin, huidig, Stip;
		float oudeSchaal;
		LocationManager locMgr;
		string tag = "myapp";
		public List<PointF> route = new List<PointF>();
		public Boolean lijstbijhouden = false;
			
		public Scherm(ContextThemeWrapper context) : base(context)
		{
			this.SetBackgroundColor(Color.Gray);
			BitmapFactory.Options opt = new BitmapFactory.Options();
			opt.InScaled = false;
			Utrecht = BitmapFactory.DecodeResource(context.Resources, Resource.Drawable.Utrecht, opt);

			KaartPositie = new PointF(this.Utrecht.Width / 2, this.Utrecht.Height / 2);//het punt linksboven
			this.Touch += RaakAan;
			huidig = new PointF(this.Width / 2, this.Height / 2);
			Stip = new PointF(Schaal * huidig.X + this.Width / 2, Schaal * huidig.Y + this.Height / 2);
			// Zet het punt voor nu even buiten de kaart omdat er nog geen gps-locatie is

		}

		public void invalidateScherm()
		{
			this.Invalidate();
		}

		protected override void OnDraw(Canvas c)
		{
			base.OnDraw(c);

			if (Schaal == 0)
				Schaal = Math.Min(((float)this.Width) / this.Utrecht.Width, ((float)this.Height) / this.Utrecht.Height);
			
			Paint verf = new Paint();
			verf.TextSize = 30;

			/* De kaart van Utrecht wordt in een matrix gezet zodat we 'm
			   op het canvas kunnen tekenen
			*/
			Matrix mat = new Matrix(); // midden van scherm heeft coord (0,0)!
			mat.PostTranslate(-KaartPositie.X, -KaartPositie.Y);
			mat.PostScale(this.Schaal, this.Schaal);
			mat.PostTranslate(this.Width / 2, this.Height / 2);
			c.DrawBitmap(this.Utrecht, mat, verf);
			verf.Color = Color.Red;

			//teken een stip op je (huidige) locatie
			//huidig = Projectie.Geo2RD(this.loc);
			c.DrawCircle(Stip.X + ((this.Utrecht.Width / 2) - KaartPositie.X) * Schaal, Stip.Y + ((this.Utrecht.Height / 2) - KaartPositie.Y) * Schaal, 10, verf);
			verf.Color = Color.Purple;
			foreach (var i in this.route)
			{
				c.DrawCircle(i.X + ((this.Utrecht.Width / 2) - KaartPositie.X) * Schaal, i.Y + ((this.Utrecht.Height / 2) - KaartPositie.Y) * Schaal, 5, verf);
			}
		}

		static float afstand(PointF p1, PointF p2)
		{
			float a = p1.X - p2.X;
			float b = p1.Y - p2.Y;
			return (float)Math.Sqrt(a * a + b * b);
		}

		public void locationChanged(Location loc)
		{
			//huidig is het punt waar je bent in pixelcoordinaten op het scherm
			//de berekening doen we in twee stappen, hier van Geo naar RD
			huidig.X = (float)(Projectie.Geo2RD(loc).X - 136000.0);
			huidig.Y = 5000 - (float)(Projectie.Geo2RD(loc).Y - 453000.0);
			//hier worden de coordinaten van RD naar schermpixels omgerekend
			huidig.X = (float)(huidig.X * 0.4 - this.Utrecht.Width / 2);
			huidig.Y = (float)(huidig.Y * 0.4 - this.Utrecht.Height / 2);

			//en we tekenen een rode stip op de huidige positie
			Stip = new PointF(Schaal * huidig.X + this.Width / 2, Schaal * huidig.Y + this.Height / 2);

			//als afstand tussen laatste element en stip groot genoeg is, voeg stip toe aan lijst
			if (lijstbijhouden)
			{
				if (afstand(Stip, route.Last()) > 4)
				{
					this.route.Add(Stip);
				}

				this.Invalidate();
			}
		}

		public void RaakAan(object o, TouchEventArgs tea)
		{
			//vinger1 raakt het scherm aan, hiervan willen we de positie weten
			v1 = new PointF(tea.Event.GetX(0), tea.Event.GetY(0));

			//pinchgedeelte
			if (tea.Event.PointerCount == 2)
			{
				//ook de tweede vinger heeft een positie
				v2 = new PointF(tea.Event.GetX(1), tea.Event.GetY(1));
				if (tea.Event.Action == MotionEventActions.Pointer2Down)
				{//zodra 2e vinger op scherm: dit zijn de coordinaten.
					s1 = v1;
					s2 = v2;
					oudeSchaal = Schaal; //en dit is de startschaal
				}
				//Als je pincht verandert de afstand tussen vingers van oud naar nieuw
				float oud = afstand(s1, s2);
				float nieuw = afstand(v1, v2);
				if (oud != 0 && nieuw != 0)
				{
					//we berekenen hoeveel er is ingezoomd dus hoeveel groter het plaatje moet worden
					float factor = nieuw / oud;
					this.Schaal = (float)(oudeSchaal * factor);
					this.Invalidate();
				}
			}

			//draggedeelte
			if (tea.Event.PointerCount == 1)
			{

				if (tea.Event.Action == MotionEventActions.Down)
				{
					// zet beginpunt
					sleepbegin = v1;
					kaartbegin = KaartPositie;
				}

				if (tea.Event.Action == MotionEventActions.Move)
				{
					//de gesleepte afstand = de verandering
					this.KaartPositie.X = kaartbegin.X - (v1.X - sleepbegin.X);
					this.KaartPositie.Y = kaartbegin.Y - (v1.Y - sleepbegin.Y);
					sleepbegin = v1;
				}

				this.Invalidate();
			}

			if (tea.Event.Action == MotionEventActions.Up)
			{
				//Als je je laatste vinger van het scherm afhaalt, schiet de schaal terug binnen deze grenzen.
				this.Schaal = Math.Max((float)0.3, Math.Min((float)3, (float)(this.Schaal)));

			}
		}

	}

	/*
Deze klasse bevat conversiemethodes tussen
 - geografische coordinaten (op de gebruikelijke WGS84 ellipsoide)
 - coordinaten in de RD (Rijksdriehoekmeting)-projectie van de Topografische Dienst Nederland/Het Kadaster

De methodes zijn gebaseerd op de beschrijving in:
    F.H. Schreutelkamp en G.L. Strang van Hees:
    "Benaderingsformules voor de transformatie tussen RD- en WGS84-kaartcoordinaten"
    in _Geodesia_  *43* (2001), pp. 64-69.
*/
	class Projectie
	{
		private const double fi0 = 52.15517440;
		private const double lam0 = 5.38720621;
		private const double x0 = 155000.00;
		private const double y0 = 463000.00;

		// Conversie van RD naar Geografisch
		// Parameter rd bevat X- en Y-coordinaat in RD-projectie
		// Resultaat bevat de latitude (breedtegraad, bijvoorbeeld 52 graden Noorderbreedte)
		//              en de longitude (lengtegraad, bijvoorbeeld 5 graden Oosterlengte)

		public static Location RD2Geo(PointF rd)
		{
			double x = (rd.X - x0) * 1E-5;
			double y = (rd.Y - y0) * 1E-5;

			double x2 = x * x;
			double x3 = x2 * x;
			double x4 = x3 * x;
			double x5 = x4 * x;
			double y2 = y * y;
			double y3 = y2 * y;
			double y4 = y3 * y;

			double fi = fi0 +
					  (3235.65389 * y
					  - 32.58297 * x2
					  - 0.24750 * y2
					  - 0.84978 * x2 * y
					  - 0.06550 * y3
					  - 0.01709 * x2 * y2
					  - 0.00738 * x
					  + 0.00530 * x4
					  - 0.00039 * x2 * y3
					  + 0.00033 * x4 * y
					  - 0.00012 * x * y
					  ) / 3600;

			double lam = lam0 +
					   (5260.52916 * x
					   + 105.94684 * x * y
					   + 2.45656 * x * y2
					   - 0.81885 * x3
					   + 0.05594 * x * y3
					   - 0.05607 * x3 * y
					   + 0.01199 * y
					   - 0.00256 * x3 * y2
					   + 0.00128 * x * y4
					   + 0.00022 * y2
					   - 0.00022 * x2
					   + 0.00026 * x5
					   ) / 3600;

			Location loc = new Location("");
			loc.Latitude = fi;
			loc.Longitude = lam;
			return loc;
		}


		// Conversie van Geografisch naar RD
		// Parameter geo bevat de latitude (breedtegraad, bijvoorbeeld 52 graden Noorderbreedte)
		//                  en de longitude (lengtegraad, bijvoorbeeld 5 graden Oosterlengte)
		// Resultaat bevat X- en Y-coordinaat in RD-projectie

		public static PointF Geo2RD(Location geo)
		{
			double fi = geo.Latitude;
			double lam = geo.Longitude;

			double dFi = 0.36 * (fi - fi0);
			double dLam = 0.36 * (lam - lam0);

			double dFi2 = dFi * dFi;
			double dFi3 = dFi2 * dFi;
			double dLam2 = dLam * dLam;
			double dLam3 = dLam2 * dLam;
			double dLam4 = dLam3 * dLam;

			double x = x0
					 + 190094.945 * dLam
					 - 11832.228 * dFi * dLam
					 - 114.221 * dFi2 * dLam
					 - 32.391 * dLam3
					 - 0.705 * dFi
					 - 2.340 * dFi3 * dLam
					 - 0.608 * dFi * dLam3
					 - 0.008 * dLam2
					 + 0.148 * dFi2 * dLam3;

			double y = y0
					 + 309056.544 * dFi
					 + 3638.893 * dLam2
					 + 73.077 * dFi2
					 - 157.984 * dFi * dLam2
					 + 59.788 * dFi3
					 + 0.433 * dLam
					 - 6.439 * dFi2 * dLam2
					 - 0.032 * dFi * dLam
					 + 0.092 * dLam4
					 - 0.054 * dFi * dLam4;

			PointF rd = new PointF((float)x, (float)y);
			return rd;
		}
	}
}