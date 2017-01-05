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
		Location huidige;
		LocationManager locMgr;

		string Provider;

		protected override void OnCreate(Bundle b)
		{
			base.OnCreate(b);

			locMgr = (LocationManager)GetSystemService(LocationService);
			Criteria criteriaForLocationService = new Criteria
			{
				Accuracy = Accuracy.Fine
			};
			IList<string> acceptableLocationProviders = locMgr.GetProviders(criteriaForLocationService, true);

			if (acceptableLocationProviders.Any())
			{
				Provider = acceptableLocationProviders.First();
			}
			else {
				Provider = string.Empty;
			}
			Log.Debug("myapp", "Using " + Provider + ".");

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

		public void begin(object o, EventArgs ea)
		{
			start.Text = "geklikt";
		}

		public void weg(object o, EventArgs ea)
		{
			clear.Text = "geklikt";
		}

		public void plek(object o, EventArgs ea)
		{
			center.Text = "geklikt";

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
			Log.Debug("myapp", "TEST");
			locMgr.RequestLocationUpdates(Provider, 1000, 1, this);
		}

		protected override void OnPause()
		{
			base.OnPause();
			locMgr.RemoveUpdates(this);
		}
	}

	public class Scherm : View
	{
		Bitmap Utrecht;
		float Schaal, Hoek;
		PointF v1, v2, s1, s2, Sleep, sleepbegin, kaartbegin, huidig;
		float oudeSchaal;
		GestureDetector draak;
		LocationManager locMgr;
		string tag = "myapp";
			
		public Scherm(ContextThemeWrapper context) : base(context)
		{
			this.SetBackgroundColor(Color.Gray);
			BitmapFactory.Options opt = new BitmapFactory.Options();
			opt.InScaled = false;
			Utrecht = BitmapFactory.DecodeResource(context.Resources, Resource.Drawable.Utrecht, opt);

			/*SensorManager sm = (SensorManager)Context.GetSystemService(Context.SensorService);
			sm.RegisterListener(this, sm.GetDefaultSensor(SensorType.Orientation), SensorDelay.Ui);
*/
			Sleep = new PointF(-this.Utrecht.Width / 2, -this.Utrecht.Height / 2);//het punt linksboven
			this.Touch += RaakAan;

			//locMgr = Context.GetSystemService(Context.LocationService) as LocationManager;
			//string Provider = LocationManager.GpsProvider;
			//locMgr.RequestLocationUpdates(Provider, 2000, 1, this);

			// Zet het punt voor nu even buiten de kaart omdat er nog geen gps-locatie is
			huidig = new PointF(this.Width, this.Height);
		}

		protected override void OnDraw(Canvas c)
		{
			base.OnDraw(c);

			if (Schaal == 0)
				Schaal = Math.Min(((float)this.Width) / this.Utrecht.Width, ((float)this.Height) / this.Utrecht.Height);
			
			Paint verf = new Paint();
			verf.TextSize = 30;
			//c.DrawText(Hoek.ToString(), 100, 20, verf);
			//c.DrawText(Schaal.ToString(), 100, 50, verf);

			Matrix mat = new Matrix(); // midden van scherm heeft coord (0,0)!
			mat.PostTranslate(Sleep.X, Sleep.Y);
			mat.PostScale(this.Schaal, this.Schaal);
			//mat.PostRotate(-this.Hoek);
			mat.PostTranslate(this.Width / 2, this.Height / 2);
			c.DrawBitmap(this.Utrecht, mat, verf);
			verf.Color = Color.Red;
			//teken een stip op je (huidige) locatie
			//huidig = Projectie.Geo2RD(this.loc);
			c.DrawCircle(Schaal * huidig.X + this.Width / 2, Schaal * huidig.Y + this.Height / 2, 10, verf);
			//(float)(this.Width / 2 + (Sleep.X + 2445 * 0.4) * Schaal), (float)(this.Height / 2 + (Sleep.Y + 1405) * Schaal)
		}

		/*public void OnSensorChanged(SensorEvent e)
		{
			this.Hoek = e.Values[0];
			this.Invalidate();
		}

		public void OnAccuracyChanged(Sensor s, SensorStatus accuracy)
		{
		}*/

		static float afstand(PointF p1, PointF p2)
		{
			float a = p1.X - p2.X;
			float b = p1.Y - p2.Y;
			return (float)Math.Sqrt(a * a + b * b);
		}

		public void locationChanged(Location loc)
		{
			huidig.X = (float)(Projectie.Geo2RD(loc).X - 136000.0);
			huidig.Y = 5000 - (float)(Projectie.Geo2RD(loc).Y - 453000.0);

			Log.Info(tag, huidig.X + " " + huidig.Y);
			huidig.X = (float)(huidig.X * 0.4 - this.Utrecht.Width / 2);
			huidig.Y = (float)(huidig.Y * 0.4 - this.Utrecht.Height / 2);

			Log.Info(tag, huidig.X + " " + huidig.Y);

			this.Invalidate();
		}





		public void RaakAan(object o, TouchEventArgs tea)
		{
			v1 = new PointF(tea.Event.GetX(0), tea.Event.GetY(0));

			//pinchgedeelte
			if (tea.Event.PointerCount == 2)
			{
				v2 = new PointF(tea.Event.GetX(1), tea.Event.GetY(1));
				if (tea.Event.Action == MotionEventActions.Pointer2Down)
				{//zodra 2e vinger op scherm: dit zijn de coordinaten.
					s1 = v1;
					s2 = v2;
					oudeSchaal = Schaal; //en dit is de startschaal
				}

				float oud = afstand(s1, s2);
				float nieuw = afstand(v1, v2);
				if (oud != 0 && nieuw != 0)
				{
					float factor = nieuw / oud;
					this.Schaal = (float)(oudeSchaal * factor);
					this.Invalidate();
				}
			}


			//draggedeelte
			if (tea.Event.PointerCount == 1)
			{
				/*if (tea.Event.Action == MotionEventActions.Down)
				{
					sleepbegin = v1;
					kaartbegin = Sleep;
					// zet begin-punt
				}

				if (tea.Event.Action == MotionEventActions.Move)
				{
					this.Sleep.X = kaartbegin.X + (v1.X - sleepbegin.X)/2;
				}*/
				//s1 = v1;
				//mat.PostTranslate(-this.Utrecht.Width / 2, -this.Utrecht.Height / 2);
				//if (tea.Event.Action == MotionEventActions.PointerIdShift);

				//this.Sleep = (-c.Utrecht.Width / 2,  -c.Utrecht.Height / 2);
				this.Invalidate();
			}

			if (tea.Event.Action == MotionEventActions.Up)
			{
				//Als je je laatste vinger van het scherm afhaalt, schiet de schaal terug binnen deze grenzen.
				this.Schaal = Math.Max((float)0.3, Math.Min((float)3, (float)(this.Schaal)));

			}
		}

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

