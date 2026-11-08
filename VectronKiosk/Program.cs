﻿﻿﻿﻿#nullable enable

using Timer = System.Windows.Forms.Timer;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace VectronKiosk
{
	public enum KioskThemeMode
	{
		Dark,
		Light
	}
	public enum KioskQualityTier
	{
		High,
		Lite
	}
	internal static class KioskSkin
	{
		public static KioskThemeMode Theme { get; set; } = KioskThemeMode.Light;

		public static KioskQualityTier Quality { get; set; } = KioskQualityTier.High;

		public static bool IsDark => Theme == KioskThemeMode.Dark;

		public static bool IsHigh => Quality == KioskQualityTier.High;

		public static Color Bg
		{
			get
			{
				if (!IsDark)
				{
					return Color.FromArgb(244, 246, 250);
				}
				return Color.FromArgb(12, 15, 23);
			}
		}

		public static Color HeaderBg
		{
			get
			{
				if (!IsDark)
				{
					return Color.FromArgb(255, 255, 255);
				}
				return Color.FromArgb(18, 23, 34);
			}
		}

		public static Color Hairline
		{
			get
			{
				if (!IsDark)
				{
					return Color.FromArgb(232, 238, 245);
				}
				return Color.FromArgb(32, 41, 58);
			}
		}

		public static Color CardBg
		{
			get
			{
				if (!IsDark)
				{
					return Color.FromArgb(255, 255, 255);
				}
				return Color.FromArgb(22, 28, 41);
			}
		}

		public static Color CardHover
		{
			get
			{
				if (!IsDark)
				{
					return Color.FromArgb(248, 250, 252);
				}
				return Color.FromArgb(30, 39, 56);
			}
		}

		public static Color CardBorder
		{
			get
			{
				if (!IsDark)
				{
					return Color.FromArgb(234, 239, 246);
				}
				return Color.FromArgb(35, 45, 66);
			}
		}

		public static Color Border
		{
			get
			{
				if (!IsDark)
				{
					return Color.FromArgb(226, 232, 240);
				}
				return Color.FromArgb(42, 54, 78);
			}
		}

		public static Color RoyalBlue => Color.FromArgb(45, 96, 255);

		public static Color Accent
		{
			get
			{
				if (!IsDark)
				{
					return RoyalBlue;
				}
				return Color.FromArgb(56, 189, 248);
			}
		}

		public static Color AccentHover
		{
			get
			{
				if (!IsDark)
				{
					return Color.FromArgb(37, 79, 222);
				}
				return Color.FromArgb(14, 165, 233);
			}
		}

		public static Color OnAccent => Color.FromArgb(255, 255, 255);

		public static Color Text
		{
			get
			{
				if (!IsDark)
				{
					return Color.FromArgb(30, 41, 59);
				}
				return Color.FromArgb(248, 250, 252);
			}
		}

		public static Color TextMuted
		{
			get
			{
				if (!IsDark)
				{
					return Color.FromArgb(100, 116, 139);
				}
				return Color.FromArgb(148, 163, 184);
			}
		}

		public static Color Gold => Color.FromArgb(245, 158, 11);

		public static Color Danger => Color.FromArgb(239, 68, 68);

		public static Color Warning => Gold;

		public static Color PillBg
		{
			get
			{
				if (!IsDark)
				{
					return Color.FromArgb(241, 245, 249);
				}
				return Color.FromArgb(26, 33, 48);
			}
		}

		public static Color InputDeep
		{
			get
			{
				if (!IsDark)
				{
					return Color.FromArgb(243, 244, 246);
				}
				return Color.FromArgb(20, 24, 33);
			}
		}

		public static (Color Bg, Color Fore) GetCategoryPastel(string? category)
		{
			string text = (category ?? "").ToLowerInvariant();
			if (text.Contains("bread") || text.Contains("pain"))
			{
				return (Bg: IsDark ? Color.FromArgb(20, 45, 70) : Color.FromArgb(240, 249, 255), Fore: Color.FromArgb(2, 132, 199));
			}
			if (text.Contains("pastr") || text.Contains("croissant") || text.Contains("viennoiserie"))
			{
				return (Bg: IsDark ? Color.FromArgb(18, 50, 40) : Color.FromArgb(240, 253, 244), Fore: Color.FromArgb(22, 163, 74));
			}
			if (text.Contains("cake") || text.Contains("gâteau") || text.Contains("dessert"))
			{
				return (Bg: IsDark ? Color.FromArgb(60, 20, 50) : Color.FromArgb(253, 242, 248), Fore: Color.FromArgb(219, 39, 119));
			}
			if (text.Contains("donut") || text.Contains("sweet") || text.Contains("sucré"))
			{
				return (Bg: IsDark ? Color.FromArgb(60, 45, 15) : Color.FromArgb(254, 252, 232), Fore: Color.FromArgb(202, 138, 4));
			}
			if (text.Contains("sandw") || text.Contains("burger") || text.Contains("snack"))
			{
				return (Bg: IsDark ? Color.FromArgb(60, 30, 15) : Color.FromArgb(255, 247, 237), Fore: Color.FromArgb(234, 88, 12));
			}
			if (text.Contains("pizza"))
			{
				return (Bg: IsDark ? Color.FromArgb(65, 20, 25) : Color.FromArgb(255, 241, 242), Fore: Color.FromArgb(225, 29, 72));
			}
			if (text.Contains("drink") || text.Contains("boisson") || text.Contains("café") || text.Contains("coffee"))
			{
				return (Bg: IsDark ? Color.FromArgb(40, 25, 65) : Color.FromArgb(245, 243, 255), Fore: Color.FromArgb(124, 58, 237));
			}
			return (Bg: IsDark ? Color.FromArgb(30, 38, 52) : Color.FromArgb(241, 245, 249), Fore: Color.FromArgb(71, 85, 105));
		}

		public static GraphicsPath RoundRect(Rectangle r, int radius)
		{
			GraphicsPath graphicsPath = new GraphicsPath();
			if (radius <= 0)
			{
				graphicsPath.AddRectangle(r);
				return graphicsPath;
			}
			int num = radius * 2;
			if (num > r.Width)
			{
				num = r.Width;
			}
			if (num > r.Height)
			{
				num = r.Height;
			}
			Rectangle rect = new Rectangle(r.X, r.Y, num, num);
			graphicsPath.AddArc(rect, 180f, 90f);
			rect.X = r.Right - num;
			graphicsPath.AddArc(rect, 270f, 90f);
			rect.Y = r.Bottom - num;
			graphicsPath.AddArc(rect, 0f, 90f);
			rect.X = r.Left;
			graphicsPath.AddArc(rect, 90f, 90f);
			graphicsPath.CloseFigure();
			return graphicsPath;
		}

		public static GraphicsPath TopRoundRect(Rectangle r, int radius)
		{
			GraphicsPath graphicsPath = new GraphicsPath();
			if (radius <= 0)
			{
				graphicsPath.AddRectangle(r);
				return graphicsPath;
			}
			int num = radius * 2;
			if (num > r.Width)
			{
				num = r.Width;
			}
			if (num > r.Height)
			{
				num = r.Height;
			}
			Rectangle rect = new Rectangle(r.X, r.Y, num, num);
			graphicsPath.AddArc(rect, 180f, 90f);
			rect.X = r.Right - num;
			graphicsPath.AddArc(rect, 270f, 90f);
			graphicsPath.AddLine(r.Right, r.Bottom, r.Left, r.Bottom);
			graphicsPath.CloseFigure();
			return graphicsPath;
		}

		public static void DrawCardShadow(Graphics g, Rectangle r, int radius, int shadowDepth = 4)
		{
			if (!IsHigh)
			{
				return;
			}
			if (IsDark)
			{
				using GraphicsPath path = RoundRect(new Rectangle(r.X, r.Y + shadowDepth / 2, r.Width, r.Height), radius);
				using SolidBrush brush = new SolidBrush(Color.FromArgb(35, 0, 0, 0));
				g.FillPath(brush, path);
			}
			else
			{
				using GraphicsPath path1 = RoundRect(new Rectangle(r.X, r.Y + shadowDepth, r.Width, r.Height), radius);
				using SolidBrush brush1 = new SolidBrush(Color.FromArgb(10, 20, 45, 90));
				g.FillPath(brush1, path1);
				using GraphicsPath path2 = RoundRect(new Rectangle(r.X, r.Y + Math.Max(1, shadowDepth / 2), r.Width, r.Height), radius);
				using SolidBrush brush2 = new SolidBrush(Color.FromArgb(18, 20, 45, 90));
				g.FillPath(brush2, path2);
			}
		}

		public static void DrawFoodContactShadow(Graphics g, int centerX, int baselineY, int width, int height)
		{
			using (SolidBrush brush = new SolidBrush(Color.FromArgb(IsDark ? 45 : 22, 0, 0, 0)))
			{
				g.FillEllipse(brush, centerX - width / 2, baselineY - height / 2, width, height);
			}
			int num = (int)((double)width * 0.65);
			int num2 = (int)((double)height * 0.55);
			using SolidBrush brush2 = new SolidBrush(Color.FromArgb(IsDark ? 70 : 35, 0, 0, 0));
			g.FillEllipse(brush2, centerX - num / 2, baselineY - num2 / 2, num, num2);
		}

		public static void DrawDottedDivider(Graphics g, int x1, int y, int x2)
		{
			using Pen pen = new Pen(IsDark ? Color.FromArgb(50, 65, 90) : Color.FromArgb(203, 213, 225), 1.5f);
			pen.DashStyle = DashStyle.Dash;
			pen.DashPattern = new float[2] { 3f, 3f };
			g.DrawLine(pen, x1, y, x2, y);
		}
	}
	internal static class KioskLang
	{
		private static readonly Dictionary<string, string> En = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
		{
			["OrderType"] = "Choose your dining experience",
			["WhereEating"] = "Where would you like to enjoy your meal?",
			["ServiceChoiceSub"] = "Prepared to order with hand-selected ingredients",
			["DineIn"] = "Dine-In",
			["DineInSub"] = "Enjoy a comfortable seat at your table",
			["DineInCta"] = "Dine-in Here →",
			["Takeaway"] = "Takeaway",
			["TakeawaySub"] = "Packed fresh and warm to take on the go",
			["TakeawayCta"] = "Takeaway →",
			["AllMenu"] = "All Menu",
			["YourOrder"] = "Your Order",
			["Subtotal"] = "Subtotal",
			["Tax"] = "Tax 10%",
			["Total"] = "TOTAL",
			["Checkout"] = "Place Order ({0})",
			["ClearCart"] = "Clear",
			["SearchPlaceholder"] = "Search something sweet on your mind...",
			["CustomerName"] = "Customer",
			["OrderNumber"] = "Order #{0}",
			["SelectTable"] = "Select Table",
			["OrderTypeDropdown"] = "Order Type",
			["NoItemSelected"] = "No Item Selected",
			["AddPromo"] = "Add Promo or Voucher",
			["PaymentMethod"] = "Payment Method",
			["PlaceOrder"] = "Place Order",
			["TrackOrder"] = "Track Order",
			["CloseOrder"] = "Close",
			["ItemsCount"] = "{0} Items",
			["Cancel"] = "Cancel",
			["Back"] = "Back",
			["Close"] = "Close",
			["Online"] = "POS Online",
			["Offline"] = "Offline Mode",
			["AccessibilityMode"] = "Accessibility",
			["UpsellTag"] = "RECOMMENDED FOR YOU",
			["UpsellTitle"] = "Complete Your Meal with a Side or Sweet!",
			["UpsellSubtitle"] = "Select any delicious add-on to include it in your order with 1 tap",
			["SkipUpsell"] = "No thanks, proceed to checkout →",
			["ProceedWithExtras"] = "Proceed with order ({0}) →",
			["UpsellFries"] = "Golden Crispy Fries",
			["UpsellFriesDesc"] = "Hand-cut crispy potatoes with sea salt",
			["UpsellDrink"] = "Fresh House Lemonade",
			["UpsellDrinkDesc"] = "Freshly squeezed lemon & crushed mint",
			["UpsellDessert"] = "Wild Berry Cheesecake",
			["UpsellDessertDesc"] = "Crispy crust with red berries coulis",
			["UpsellCoffee"] = "Pure Origin Espresso",
			["UpsellCoffeeDesc"] = "Freshly roasted single origin beans",
			["UpsellAdded"] = "Added!",
			["TapToOrder"] = "TOUCH TO ORDER →",
			["AttractTitle"] = "ORDER IN JUST A FEW TAPS",
			["AttractSub"] = "Dine-in or Takeaway • Handcrafted to order",
			["ChefPick"] = "CHEF'S PICK",
			["TimeoutTitle"] = "Are you still there?",
			["ContinueOrder"] = "Continue Order",
			["OrderSuccess"] = "ORDER SENT SUCCESSFULLY!",
			["OrderSentDesc"] = "Your order has been sent to our kitchen team.",
			["AutoReturn"] = "Returning in {0}s",
			["Done"] = "Done"
		};

		private static readonly Dictionary<string, string> Fr = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
		{
			["OrderType"] = "Choisissez votre mode de dégustation",
			["WhereEating"] = "Où souhaitez-vous déguster votre repas ?",
			["ServiceChoiceSub"] = "Préparé à la minute avec des ingrédients rigoureusement sélectionnés",
			["DineIn"] = "Sur Place",
			["DineInSub"] = "Dégustez confortablement dans notre salle",
			["DineInCta"] = "Déguster sur place →",
			["Takeaway"] = "À Emporter",
			["TakeawaySub"] = "Votre commande emballée avec soin prête à emporter",
			["TakeawayCta"] = "Prendre à emporter →",
			["AllMenu"] = "Tout le Menu",
			["YourOrder"] = "Votre Commande",
			["Subtotal"] = "Sous-total",
			["Tax"] = "TVA 10%",
			["Total"] = "TOTAL",
			["Checkout"] = "Valider ({0})",
			["ClearCart"] = "Vider",
			["SearchPlaceholder"] = "Rechercher une douceur ou un plat...",
			["CustomerName"] = "Client",
			["OrderNumber"] = "Commande #{0}",
			["SelectTable"] = "Choisir table",
			["OrderTypeDropdown"] = "Type de repas",
			["NoItemSelected"] = "Aucun article sélectionné",
			["AddPromo"] = "Code promo ou bon d'achat",
			["PaymentMethod"] = "Moyen de paiement",
			["PlaceOrder"] = "Valider la commande",
			["TrackOrder"] = "Suivre commande",
			["CloseOrder"] = "Fermer",
			["ItemsCount"] = "{0} articles",
			["Cancel"] = "Annuler",
			["Back"] = "Retour",
			["Close"] = "Fermer",
			["Online"] = "POS Connecté",
			["Offline"] = "Mode Hors-ligne",
			["AccessibilityMode"] = "Accessibilité",
			["UpsellTag"] = "OFFRE COMPLÉMENTAIRE",
			["UpsellTitle"] = "Envie d'un petit plus pour accompagner votre repas ?",
			["UpsellSubtitle"] = "Sélectionnez un article pour l'ajouter en un clic à votre commande",
			["SkipUpsell"] = "Non merci, passer au paiement →",
			["ProceedWithExtras"] = "Finaliser ma commande ({0}) →",
			["UpsellFries"] = "Frites Dorées Croustillantes",
			["UpsellFriesDesc"] = "Pommes de terre croustillantes à la fleur de sel",
			["UpsellDrink"] = "Limonade Fraîche Maison",
			["UpsellDrinkDesc"] = "Citrons pressés minute et menthe fraîche",
			["UpsellDessert"] = "Cheesecake aux Fruits Rouges",
			["UpsellDessertDesc"] = "Coulis de baies sauvages sur sablé doré",
			["UpsellCoffee"] = "Espresso Pur Origine",
			["UpsellCoffeeDesc"] = "Sélection torréfiée aux arômes intenses",
			["UpsellAdded"] = "Ajouté !",
			["TapToOrder"] = "TOUCHER POUR COMMANDER →",
			["AttractTitle"] = "COMMANDEZ ICI EN QUELQUES TOUCHES",
			["AttractSub"] = "Sur place ou à emporter • Préparé à la commande",
			["ChefPick"] = "SÉLECTION DU CHEF",
			["TimeoutTitle"] = "Êtes-vous toujours là ?",
			["ContinueOrder"] = "Continuer ma commande",
			["OrderSuccess"] = "COMMANDE ENVOYÉE AVEC SUCCÈS !",
			["OrderSentDesc"] = "Votre commande est transmise directement en cuisine.",
			["AutoReturn"] = "Retour automatique dans {0}s",
			["Done"] = "Terminer"
		};

		private static readonly Dictionary<string, string> Ar = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
		{
			["OrderType"] = "اختر طريقة استلام طلبك",
			["WhereEating"] = "أين ترغب في تناول وجبتك؟",
			["ServiceChoiceSub"] = "وجبات طازجة محضرة بأجود المكونات المختارة بعناية",
			["DineIn"] = "تناول في المطعم",
			["DineInSub"] = "استمتع بوجبتك على طاولتك المفضلة",
			["DineInCta"] = "تناول في المطعم ←",
			["Takeaway"] = "طلب سفري",
			["TakeawaySub"] = "طلبك مغلف بعناية وجاهز للاستلام الفوري",
			["TakeawayCta"] = "استلام سفري ←",
			["AllMenu"] = "كل القائمة",
			["YourOrder"] = "سلة طلبك",
			["Subtotal"] = "المجموع الجزئي",
			["Tax"] = "الضريبة 10%",
			["Total"] = "المجموع الكلي",
			["Checkout"] = "تأكيد الطلب ({0})",
			["ClearCart"] = "إفراغ",
			["SearchPlaceholder"] = "ابحث عن طبق أو مشروب...",
			["CustomerName"] = "الزبون",
			["OrderNumber"] = "طلب رقم #{0}",
			["SelectTable"] = "اختر طاولة",
			["OrderTypeDropdown"] = "نوع الطلب",
			["NoItemSelected"] = "لا توجد أطباق مختارة بعد",
			["AddPromo"] = "إضافة كود خصم",
			["PaymentMethod"] = "طريقة الدفع",
			["PlaceOrder"] = "تأكيد الطلب",
			["TrackOrder"] = "متابعة الطلب",
			["CloseOrder"] = "إغلاق",
			["ItemsCount"] = "{0} منتجات",
			["Cancel"] = "إلغاء",
			["Back"] = "رجوع",
			["Close"] = "إغلاق",
			["Online"] = "متصل بالنظام",
			["Offline"] = "وضع غير متصل",
			["AccessibilityMode"] = "سهولة الوصول",
			["UpsellTag"] = "اقتراحات إضافية لك",
			["UpsellTitle"] = "أكمل وجبتك بمشروب أو حلوى لذيذة!",
			["UpsellSubtitle"] = "انقر نقرة واحدة لإضافة طبق جانبي أو تحلية",
			["SkipUpsell"] = "لا شكراً، المتابعة للدفع ←",
			["ProceedWithExtras"] = "متابعة الطلب مع الإضافات ({0}) ←",
			["UpsellFries"] = "بطاطس مقرمشة ذهبية",
			["UpsellFriesDesc"] = "بطاطس مقرمشة مع ملح البحر",
			["UpsellDrink"] = "عصير ليموناضة طازج",
			["UpsellDrinkDesc"] = "ليمون طازج مع نعناع منعش",
			["UpsellDessert"] = "تشيز كيك التوت المشكل",
			["UpsellDessertDesc"] = "قاعدة بسكويت مقرمشة مع توت بري",
			["UpsellCoffee"] = "إسبريسو أصلي",
			["UpsellCoffeeDesc"] = "بن محمص فاخر بنكهة غنية",
			["UpsellAdded"] = "تمت الإضافة!",
			["TapToOrder"] = "المس الشاشة للبدء في الطلب ←",
			["AttractTitle"] = "اطلب وجبتك في لحظات معدودة",
			["AttractSub"] = "محلي أو سفري • تحضير فوري طازج",
			["ChefPick"] = "اختيار الشيف",
			["TimeoutTitle"] = "هل ما زلت بحاجة للطلب؟",
			["ContinueOrder"] = "متابعة الطلب",
			["OrderSuccess"] = "تم إرسال طلبك بنجاح!",
			["OrderSentDesc"] = "تم إرسال طلبك مباشرة إلى المطبخ لتحضيره.",
			["AutoReturn"] = "العودة للشاشة الرئيسية خلال {0} ثوانٍ",
			["Done"] = "تم"
		};

		public static string Current { get; set; } = "fr";

		public static string T(string key, params object[] args)
		{
			string current = Current;
			Dictionary<string, string> dictionary;
			if (current == "fr")
			{
				dictionary = Fr;
			}
			else
			{
				dictionary = ((!(current == "ar")) ? En : Ar);
			}
			if (dictionary.TryGetValue(key, out var value))
			{
				if (args == null || args.Length == 0)
				{
					return value;
				}
				return string.Format(value, args);
			}
			if (En.TryGetValue(key, out string value2))
			{
				if (args == null || args.Length == 0)
				{
					return value2;
				}
				return string.Format(value2, args);
			}
			return key;
		}

		public static string FormatPrice(decimal price, string currency)
		{
			return $"{price:F2} {currency}";
		}
	}
		internal static class KioskDiscovery
	{
		private static readonly HttpClient Http = new HttpClient { Timeout = TimeSpan.FromMilliseconds(500) };

		public static async Task<string?> FindPosServerAsync(string? knownUrl = null, string apiKey = "")
		{
			if (!string.IsNullOrWhiteSpace(knownUrl) && knownUrl.StartsWith("http", StringComparison.OrdinalIgnoreCase))
			{
				if (await ProbeServerAsync(knownUrl, apiKey).ConfigureAwait(false))
				{
					return knownUrl.TrimEnd('/');
				}
			}

			string local = "http://127.0.0.1:8080";
			if (await ProbeServerAsync(local, apiKey).ConfigureAwait(false))
			{
				return local;
			}

			List<string> subnets = new List<string>();
			try
			{
				foreach (NetworkInterface ni in NetworkInterface.GetAllNetworkInterfaces())
				{
					if (ni.OperationalStatus != OperationalStatus.Up) continue;
					if (ni.NetworkInterfaceType == NetworkInterfaceType.Loopback) continue;

					IPInterfaceProperties ipProps = ni.GetIPProperties();
					foreach (UnicastIPAddressInformation ua in ipProps.UnicastAddresses)
					{
						if (ua.Address.AddressFamily == AddressFamily.InterNetwork)
						{
							string ip = ua.Address.ToString();
							if (ip.StartsWith("127.")) continue;
							int lastDot = ip.LastIndexOf('.');
							if (lastDot > 0)
							{
								string prefix = ip.Substring(0, lastDot + 1);
								if (!subnets.Contains(prefix)) subnets.Add(prefix);
							}
						}
					}
				}
			}
			catch { }

			if (subnets.Count == 0)
			{
				subnets.Add("192.168.1.");
				subnets.Add("192.168.0.");
			}

			foreach (string prefix in subnets)
			{
				List<string> candidates = Enumerable.Range(1, 254).Select(i => $"http://{prefix}{i}:8080").ToList();
				for (int i = 0; i < candidates.Count; i += 32)
				{
					var batch = candidates.Skip(i).Take(32);
					var tasks = batch.Select(async host =>
					{
						if (await ProbeServerAsync(host, apiKey).ConfigureAwait(false))
						{
							return host;
						}
						return null;
					}).ToList();

					string?[] results = await Task.WhenAll(tasks).ConfigureAwait(false);
					string? match = results.FirstOrDefault(r => !string.IsNullOrEmpty(r));
					if (match != null)
					{
						return match;
					}
				}
			}

			return null;
		}

		private static async Task<bool> ProbeServerAsync(string host, string apiKey)
		{
			try
			{
				using CancellationTokenSource cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(450));
				string url = host.TrimEnd('/') + "/api/kiosk/bootstrap";
				if (!string.IsNullOrWhiteSpace(apiKey)) url += "?key=" + Uri.EscapeDataString(apiKey);
				using HttpRequestMessage req = new HttpRequestMessage(HttpMethod.Get, url);
				using HttpResponseMessage resp = await Http.SendAsync(req, cts.Token).ConfigureAwait(false);
				string text = await resp.Content.ReadAsStringAsync().ConfigureAwait(false);
				if (text.Contains("restaurant_name") || text.Contains("\"ok\":") || text.Contains("Invalid API key"))
				{
					return true;
				}
			}
			catch { }

			try
			{
				using CancellationTokenSource cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(450));
				string url = host.TrimEnd('/') + "/api/tablet/bootstrap";
				if (!string.IsNullOrWhiteSpace(apiKey)) url += "?key=" + Uri.EscapeDataString(apiKey);
				using HttpRequestMessage req = new HttpRequestMessage(HttpMethod.Get, url);
				using HttpResponseMessage resp = await Http.SendAsync(req, cts.Token).ConfigureAwait(false);
				string text = await resp.Content.ReadAsStringAsync().ConfigureAwait(false);
				if (text.Contains("restaurant_name") || text.Contains("\"ok\":") || text.Contains("Invalid API key"))
				{
					return true;
				}
			}
			catch { }

			return false;
		}
	}

	internal sealed class KioskConfig
	{
		private static readonly string ConfigPath = Path.Combine(AppContext.BaseDirectory, "kiosk-config.json");

		public string ServerUrl { get; set; } = "http://127.0.0.1:8080";

		public string ApiKey { get; set; } = "";

		public string Language { get; set; } = "fr";

		public string Theme { get; set; } = "Light";

		public string GraphicsQuality { get; set; } = "High";

		public string KioskName { get; set; } = "Kiosk 1";

		public int IdleTimeoutSeconds { get; set; } = 90;

		public string ManagerPin { get; set; } = "1234";

		public string LockscreenBackgroundMode { get; set; } = "Products";

		public string LockscreenCustomFolder { get; set; } = "";

		public bool IsConfigured { get; set; }

		public static KioskConfig Load()
		{
			try
			{
				if (File.Exists(ConfigPath))
				{
					KioskConfig kioskConfig = JsonSerializer.Deserialize<KioskConfig>(File.ReadAllText(ConfigPath));
					if (kioskConfig != null)
					{
						return kioskConfig;
					}
				}
			}
			catch
			{
			}
			return new KioskConfig();
		}

		public void Save()
		{
			try
			{
				string contents = JsonSerializer.Serialize(this, new JsonSerializerOptions
				{
					WriteIndented = true
				});
				File.WriteAllText(ConfigPath, contents);
			}
			catch
			{
			}
		}
	}
	internal sealed class KioskProduct
	{
		public string Name { get; set; } = "";

		public string Category { get; set; } = "";

		public string Family { get; set; } = "";

		public decimal Price { get; set; }

		public string Color { get; set; } = "#2D60FF";

		public string Image { get; set; } = "";

		public bool OutOfStock { get; set; }

		public string Description { get; set; } = "";

		public string Badge { get; set; } = "";
	}
	internal static class CatalogImageCache
	{
		private static readonly Dictionary<string, Image> MemCache;

		private static readonly HttpClient Http;

		private static readonly string CacheDir;

		private static readonly HashSet<string> PendingDownloads = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

		private static readonly Dictionary<string, List<Action>> PendingCallbacks = new Dictionary<string, List<Action>>(StringComparer.OrdinalIgnoreCase);

		private static string? dataImagesFolder;

		private static string? legacyImagesFolder;

		public static string? DataImagesFolder => dataImagesFolder;

		static CatalogImageCache()
		{
			MemCache = new Dictionary<string, Image>(StringComparer.OrdinalIgnoreCase);
			Http = new HttpClient
			{
				Timeout = TimeSpan.FromSeconds(5.0)
			};
			CacheDir = Path.Combine(AppContext.BaseDirectory, "images", "cache");
			dataImagesFolder = null;
			legacyImagesFolder = null;
			try
			{
				Directory.CreateDirectory(CacheDir);
			}
			catch
			{
			}
		}

		public static void SetDatabaseFolder(string dbDir)
		{
			try
			{
				dataImagesFolder = Path.Combine(dbDir, "images");
				string directoryName = Path.GetDirectoryName(dbDir.TrimEnd('\\', '/'));
				if (!string.IsNullOrEmpty(directoryName))
				{
					legacyImagesFolder = Path.Combine(directoryName, "images");
				}
			}
			catch
			{
			}
		}

		public static string? ResolveLocalDatabaseFolder()
		{
			List<string> candidates = new List<string>
			{
				Path.Combine(AppContext.BaseDirectory, "client database"),
				Path.Combine(AppContext.BaseDirectory, "..", "client database"),
				Path.Combine(AppContext.BaseDirectory, "..", "..", "client database"),
				Path.Combine(AppContext.BaseDirectory, "publish-win10-x64", "client database")
			};
			foreach (var c in candidates)
			{
				try
				{
					if (Directory.Exists(c) && File.Exists(Path.Combine(c, "products.json")))
					{
						return Path.GetFullPath(c);
					}
				}
				catch { }
			}
			return null;
		}

		public static string? ResolveLocalImagePath(string? stored)
		{
			if (string.IsNullOrWhiteSpace(stored))
			{
				return null;
			}
			if (Path.IsPathRooted(stored) && File.Exists(stored))
			{
				return stored;
			}
			string fileName = Path.GetFileName(stored);
			if (string.IsNullOrWhiteSpace(fileName))
			{
				return null;
			}
			List<string> list = new List<string>();
			if (!string.IsNullOrEmpty(dataImagesFolder))
			{
				list.Add(dataImagesFolder);
			}
			if (!string.IsNullOrEmpty(legacyImagesFolder))
			{
				list.Add(legacyImagesFolder);
			}
			string? asmDir = null;
#pragma warning disable IL3000
			try { asmDir = Path.GetDirectoryName(typeof(CatalogImageCache).Assembly.Location); } catch {}
#pragma warning restore IL3000
			if (!string.IsNullOrEmpty(asmDir))
			{
				list.Add(Path.Combine(asmDir, "client database", "images"));
				list.Add(Path.Combine(asmDir, "images", "kiosk"));
				list.Add(Path.Combine(asmDir, "images"));
				list.Add(Path.Combine(asmDir, "images", "cache"));
			}
			string baseDirectory = AppContext.BaseDirectory;
			list.Add(Path.Combine(baseDirectory, "client database", "images"));
			list.Add(Path.Combine(baseDirectory, "images"));
			list.Add(Path.Combine(baseDirectory, "images", "kiosk"));
			list.Add(Path.Combine(baseDirectory, "images", "cache"));
			for (int i = 0; i < 6; i++)
			{
				try
				{
					string fullPath = Path.GetFullPath(Path.Combine(baseDirectory, new string('.', i * 2 + 1).Replace(".", "..\\")));
					list.Add(Path.Combine(fullPath, "client database", "images"));
					list.Add(Path.Combine(fullPath, "images"));
					list.Add(Path.Combine(fullPath, "publish-win10-x64", "client database", "images"));
					list.Add(Path.Combine(fullPath, "publish-win10-x64", "images"));
					list.Add(Path.Combine(fullPath, "tester-release", "Windows", "images"));
				}
				catch
				{
				}
			}
			foreach (string item in list.Distinct(StringComparer.OrdinalIgnoreCase))
			{
				if (!string.IsNullOrEmpty(item) && Directory.Exists(item))
				{
					string text = Path.Combine(item, fileName);
					if (File.Exists(text))
					{
						return text;
					}
				}
			}
			return null;
		}

		public static Image? GetImage(string imageName, string serverUrl, string apiKey = "", Action? onLoaded = null)
		{
			if (string.IsNullOrWhiteSpace(imageName))
			{
				return null;
			}
			string key = Path.GetFileName(imageName);
			lock (MemCache)
			{
				if (MemCache.TryGetValue(key, out Image value))
				{
					return value;
				}
			}
			string text = ResolveLocalImagePath(imageName);
			if (text != null && File.Exists(text))
			{
				try
				{
					using MemoryStream stream = new MemoryStream(File.ReadAllBytes(text));
					using Bitmap original = new Bitmap(stream);
					Bitmap bitmap = new Bitmap(original);
					lock (MemCache)
					{
						MemCache[key] = bitmap;
					}
					return bitmap;
				}
				catch
				{
				}
			}
			string cachedFile = Path.Combine(CacheDir, key);
			if (File.Exists(cachedFile))
			{
				try
				{
					byte[] raw = File.ReadAllBytes(cachedFile);
					if (raw.Length > 0 && raw[0] == '{')
					{
						bool recovered = false;
						try
						{
							using var doc = JsonDocument.Parse(raw);
							if (doc.RootElement.TryGetProperty("data", out var dProp))
							{
								string? b64 = dProp.GetString();
								if (!string.IsNullOrWhiteSpace(b64))
								{
									raw = Convert.FromBase64String(b64);
									File.WriteAllBytes(cachedFile, raw);
									recovered = true;
								}
							}
						}
						catch { }
						if (!recovered)
						{
							try { File.Delete(cachedFile); } catch { }
							raw = Array.Empty<byte>();
						}
					}
					if (raw.Length > 0)
					{
						using MemoryStream stream2 = new MemoryStream(raw);
						using Bitmap original2 = new Bitmap(stream2);
						Bitmap bitmap2 = new Bitmap(original2);
						lock (MemCache)
						{
							MemCache[key] = bitmap2;
						}
						return bitmap2;
					}
				}
				catch
				{
					try { File.Delete(cachedFile); } catch { }
				}
			}
			if (string.IsNullOrWhiteSpace(serverUrl) || !serverUrl.StartsWith("http", StringComparison.OrdinalIgnoreCase))
			{
				return null;
			}
			if (onLoaded != null)
			{
				lock (PendingCallbacks)
				{
					if (!PendingCallbacks.TryGetValue(key, out var list))
					{
						list = new List<Action>();
						PendingCallbacks[key] = list;
					}
					list.Add(onLoaded);
				}
			}
			lock (PendingDownloads)
			{
				if (!PendingDownloads.Add(key))
				{
					return null;
				}
			}
			Task.Run(async () =>
			{
				try
				{
					string text2 = serverUrl.TrimEnd('/') + "/api/kiosk/image?name=" + Uri.EscapeDataString(key);
					if (!string.IsNullOrWhiteSpace(apiKey))
					{
						text2 = text2 + "&key=" + Uri.EscapeDataString(apiKey);
					}
					using HttpResponseMessage httpResponseMessage = await Http.GetAsync(text2).ConfigureAwait(false);
					if (httpResponseMessage.IsSuccessStatusCode)
					{
						byte[] array = await httpResponseMessage.Content.ReadAsByteArrayAsync().ConfigureAwait(false);
						if (array != null && array.Length != 0)
						{
							byte[]? imgBytes = null;
							if (array[0] == '{' || array[0] == '[' ||
							    (httpResponseMessage.Content.Headers.ContentType?.MediaType?.Contains("json") ?? false))
							{
								try
								{
									using var doc = JsonDocument.Parse(array);
									if (doc.RootElement.TryGetProperty("ok", out var okProp) && okProp.GetBoolean() &&
									    doc.RootElement.TryGetProperty("data", out var dataProp))
									{
										string? b64 = dataProp.GetString();
										if (!string.IsNullOrWhiteSpace(b64))
										{
											imgBytes = Convert.FromBase64String(b64);
										}
									}
								}
								catch { }
							}
							else
							{
								imgBytes = array;
							}

							if (imgBytes != null && imgBytes.Length > 0)
							{
								try
								{
									File.WriteAllBytes(cachedFile, imgBytes);
								}
								catch { }
								using MemoryStream stream3 = new MemoryStream(imgBytes);
								using Bitmap original3 = new Bitmap(stream3);
								Bitmap value2 = new Bitmap(original3);
								lock (MemCache)
								{
									MemCache[key] = value2;
								}
							}
						}
					}
				}
				catch { }
				finally
				{
					List<Action>? cbs = null;
					lock (PendingDownloads)
					{
						PendingDownloads.Remove(key);
					}
					lock (PendingCallbacks)
					{
						if (PendingCallbacks.TryGetValue(key, out var list))
						{
							cbs = new List<Action>(list);
							PendingCallbacks.Remove(key);
						}
					}
					if (cbs != null)
					{
						foreach (var action in cbs)
						{
							try { action(); } catch { }
						}
					}
				}
			});
			return null;
		}
	}
	internal sealed class LockScreenPanel : Panel
	{
		private readonly KioskMainForm mainForm;

		private readonly Timer billboardTimer = new Timer();

		private readonly Timer pulseTimer = new Timer();

		private int currentSlideIndex;

		private float pulseScale = 1f;

		private bool pulseGrowing = true;

		private bool lastIsPortrait;

		private readonly List<Image> promoImages = new List<Image>();

		private static readonly List<Image> cachedPromoImages = new List<Image>();

		public static void ClearCache() => cachedPromoImages.Clear();

		public LockScreenPanel(KioskMainForm form)
		{
			mainForm = form;
			Dock = DockStyle.Fill;
			DoubleBuffered = true;
			BackColor = KioskSkin.Bg;
			lastIsPortrait = (Height > Width);
			LoadPromoImages();
			billboardTimer.Interval = 4500;
			billboardTimer.Tick += (object s, EventArgs e) =>
			{
				if (promoImages.Count > 1)
				{
					currentSlideIndex = (currentSlideIndex + 1) % promoImages.Count;
					Invalidate();
				}
			};
			pulseTimer.Interval = 40;
			pulseTimer.Tick += (object s, EventArgs e) =>
			{
				if (pulseGrowing)
				{
					pulseScale += 0.005f;
					if (pulseScale >= 1.05f)
					{
						pulseGrowing = false;
					}
				}
				else
				{
					pulseScale -= 0.005f;
					if (pulseScale <= 0.98f)
					{
						pulseGrowing = true;
					}
				}
				Invalidate();
			};
			if (!KioskMainForm.IsHeadlessSnapshot)
			{
				billboardTimer.Start();
				pulseTimer.Start();
			}
			Click += (object s, EventArgs e) =>
			{
				mainForm.WakeFromLockScreen();
			};
		}

		protected override void OnResize(EventArgs eventargs)
		{
			base.OnResize(eventargs);
			bool currentPortrait = Height > Width;
			if (currentPortrait != lastIsPortrait && Width > 0 && Height > 0)
			{
				lastIsPortrait = currentPortrait;
				ClearCache();
				LoadPromoImages();
				Invalidate();
			}
		}

		public void LoadPromoImages()
		{
			promoImages.Clear();
			if (cachedPromoImages.Count > 0)
			{
				promoImages.AddRange(cachedPromoImages);
				return;
			}
			try
			{
				bool isPortrait = Height > Width;
				int slideW = isPortrait ? 1080 : 1920;
				int slideH = isPortrait ? 1920 : 1080;
				if (Width > 0 && Height > 0)
				{
					slideW = Width;
					slideH = Height;
				}

				string mode = mainForm?.Config?.LockscreenBackgroundMode ?? "Products";
				string customFolder = mainForm?.Config?.LockscreenCustomFolder ?? "";

				if (string.Equals(mode, "CustomFolder", StringComparison.OrdinalIgnoreCase) &&
				    !string.IsNullOrWhiteSpace(customFolder) &&
				    Directory.Exists(customFolder))
				{
					var files = Directory.GetFiles(customFolder, "*.*")
						.Where(s => s.EndsWith(".png", StringComparison.OrdinalIgnoreCase) ||
						            s.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase) ||
						            s.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase) ||
						            s.EndsWith(".bmp", StringComparison.OrdinalIgnoreCase))
						.ToList();

					foreach (var file in files)
					{
						try
						{
							using MemoryStream ms = new MemoryStream(File.ReadAllBytes(file));
							using Bitmap bmp = new Bitmap(ms);
							float scale = Math.Min((float)slideW / (float)bmp.Width, (float)slideH / (float)bmp.Height);
							if (scale < 1f)
							{
								int dw = (int)((float)bmp.Width * scale);
								int dh = (int)((float)bmp.Height * scale);
								Bitmap resized = new Bitmap(dw, dh);
								using (Graphics g = Graphics.FromImage(resized))
								{
									g.InterpolationMode = InterpolationMode.Bilinear;
									g.DrawImage(bmp, 0, 0, dw, dh);
								}
								promoImages.Add(resized);
							}
							else
							{
								promoImages.Add(new Bitmap(bmp));
							}
							if (promoImages.Count >= 8) break;
						}
						catch { }
					}
				}
				else if (string.Equals(mode, "Products", StringComparison.OrdinalIgnoreCase) &&
				         mainForm?.Products != null && mainForm.Products.Count > 0)
				{
					var withImages = mainForm.Products
						.Where(p => !string.IsNullOrWhiteSpace(p.Image))
						.OrderBy(p => p.OutOfStock ? 1 : 0)
						.Take(8)
						.ToList();

					var candidates = withImages.Count > 0
						? withImages
						: mainForm.Products.Where(p => !p.OutOfStock).Take(8).ToList();

					foreach (var prod in candidates)
					{
						Image? realImg = null;
						if (!string.IsNullOrWhiteSpace(prod.Image))
						{
							realImg = CatalogImageCache.GetImage(prod.Image, mainForm.Config.ServerUrl, mainForm.Config.ApiKey, () =>
							{
								try
								{
									if (IsHandleCreated && !IsDisposed)
									{
										BeginInvoke((Action)(() =>
										{
											ClearCache();
											LoadPromoImages();
											Invalidate();
										}));
									}
								}
								catch { }
							});
						}

						try
						{
							Bitmap slide = GenerateProductHeroSlide(slideW, slideH, prod, realImg, isPortrait, mainForm.RestaurantName, mainForm.Currency);
							promoImages.Add(slide);
						}
						catch { }
					}
				}

				if (promoImages.Count == 0)
				{
					List<string> list = new List<string>
					{
						Path.Combine(AppContext.BaseDirectory, "images", "kiosk"),
						Path.Combine(AppContext.BaseDirectory, "images")
					};
					foreach (string item in list)
					{
						if (Directory.Exists(item))
						{
							foreach (string item2 in Directory.GetFiles(item, "*.*")
								.Where(s => s.EndsWith(".png", StringComparison.OrdinalIgnoreCase) || s.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase) || s.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase)))
							{
								try
								{
									using MemoryStream stream = new MemoryStream(File.ReadAllBytes(item2));
									using Bitmap bitmap = new Bitmap(stream);
									float num = Math.Min((float)slideW / (float)bitmap.Width, (float)slideH / (float)bitmap.Height);
									if (num < 1f)
									{
										int num2 = (int)((float)bitmap.Width * num);
										int num3 = (int)((float)bitmap.Height * num);
										Bitmap bitmap2 = new Bitmap(num2, num3);
										using (Graphics graphics = Graphics.FromImage(bitmap2))
										{
											graphics.InterpolationMode = InterpolationMode.Bilinear;
											graphics.DrawImage(bitmap, 0, 0, num2, num3);
										}
										promoImages.Add(bitmap2);
									}
									else
									{
										promoImages.Add(new Bitmap(bitmap));
									}
									if (promoImages.Count >= 4) break;
								}
								catch { }
							}
						}
						if (promoImages.Count >= 4) break;
					}
				}
			}
			catch { }

			if (promoImages.Count == 0)
			{
				try
				{
					bool isPortrait = Height > Width;
					promoImages.Add(GenerateFallbackPromoImage(isPortrait ? 1080 : 1920, isPortrait ? 1920 : 1080));
				}
				catch { }
			}
			if (cachedPromoImages.Count == 0 && promoImages.Count > 0)
			{
				cachedPromoImages.AddRange(promoImages);
			}
		}

		private static Bitmap GenerateProductHeroSlide(int w, int h, KioskProduct prod, Image? realImage, bool isPortrait, string restaurantName, string currency)
		{
			Bitmap bmp = new Bitmap(w, h);
			using Graphics g = Graphics.FromImage(bmp);
			g.SmoothingMode = SmoothingMode.AntiAlias;
			g.InterpolationMode = InterpolationMode.HighQualityBicubic;
			g.PixelOffsetMode = PixelOffsetMode.HighQuality;

			if (isPortrait)
			{
				using (LinearGradientBrush bgBrush = new LinearGradientBrush(new Point(0, 0), new Point(0, h),
					Color.FromArgb(248, 250, 253), Color.FromArgb(232, 238, 248)))
				{
					g.FillRectangle(bgBrush, 0, 0, w, h);
				}

				using (SolidBrush glow = new SolidBrush(Color.FromArgb(30, 45, 96, 255)))
				{
					g.FillEllipse(glow, w / 4 - 50, (int)(h * 0.15f), w / 2 + 100, w / 2 + 100);
				}

			int foodBoxW = Math.Min(720, w - 80);
			int foodBoxH = (int)(h * 0.38f);
			int foodX = (w - foodBoxW) / 2;
			int foodY = (int)(h * 0.16f);

				KioskSkin.DrawFoodContactShadow(g, w / 2, foodY + foodBoxH + 12, (int)(foodBoxW * 0.85f), 38);

				if (realImage != null)
				{
					float scale = Math.Min((float)foodBoxW / realImage.Width, (float)foodBoxH / realImage.Height);
					int drawW = (int)(realImage.Width * scale);
					int drawH = (int)(realImage.Height * scale);
					int drawX = (w - drawW) / 2;
					int drawY = foodY + (foodBoxH - drawH) / 2;
					g.DrawImage(realImage, new Rectangle(drawX, drawY, drawW, drawH));
				}
				else
				{
					Rectangle foodRect = new Rectangle(foodX, foodY, foodBoxW, foodBoxH);
					ProductCardPanel.DrawFoodVectorIllustration(g, foodRect, prod.Category, prod.Name, true);
				}

			int cardW = Math.Min(760, w - 80);
			int cardH = 135;
			int cardX = (w - cardW) / 2;
			int cardY = foodY + foodBoxH + 50;

				using (GraphicsPath cardPath = KioskSkin.RoundRect(new Rectangle(cardX, cardY, cardW, cardH), 24))
				{
					using (SolidBrush cardBg = new SolidBrush(Color.FromArgb(230, 255, 255, 255)))
					{
						g.FillPath(cardBg, cardPath);
					}
					using (Pen borderPen = new Pen(Color.FromArgb(170, 218, 225, 233), 1.5f))
					{
						g.DrawPath(borderPen, cardPath);
					}
				}

				var pastel = KioskSkin.GetCategoryPastel(prod.Category);
				using (GraphicsPath catPill = KioskSkin.RoundRect(new Rectangle(cardX + 28, cardY + 22, 130, 28), 8))
				{
					using (SolidBrush catBg = new SolidBrush(pastel.Bg))
					{
						g.FillPath(catBg, catPill);
					}
				}
				using (Font catFont = new Font("Segoe UI Bold", 10f))
				using (SolidBrush catText = new SolidBrush(pastel.Fore))
				{
					StringFormat sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
					g.DrawString((prod.Category ?? "MENU").ToUpperInvariant(), catFont, catText, new Rectangle(cardX + 28, cardY + 22, 130, 28), sf);
				}

				using (Font titleFont = new Font("Segoe UI Bold", 20f))
				using (SolidBrush titleBrush = new SolidBrush(Color.FromArgb(26, 32, 44)))
				{
					g.DrawString(prod.Name, titleFont, titleBrush, new PointF(cardX + 28, cardY + 62));
				}

				using (Font priceFont = new Font("Segoe UI Bold", 24f))
				using (SolidBrush priceBrush = new SolidBrush(Color.FromArgb(45, 96, 255)))
				{
					StringFormat sfRight = new StringFormat { Alignment = StringAlignment.Far, LineAlignment = StringAlignment.Center };
					g.DrawString(KioskLang.FormatPrice(prod.Price, currency), priceFont, priceBrush, new Rectangle(cardX + cardW - 240, cardY + 30, 215, 75), sfRight);
				}
			}
			else
			{
				using (LinearGradientBrush bgBrush = new LinearGradientBrush(new Point(0, 0), new Point(w, h),
					Color.FromArgb(248, 246, 242), Color.FromArgb(232, 226, 218)))
				{
					g.FillRectangle(bgBrush, 0, 0, w, h);
				}

				using (SolidBrush glow = new SolidBrush(Color.FromArgb(45, 255, 220, 150)))
				{
					g.FillEllipse(glow, w / 4, h / 6, 700, 700);
				}

				Rectangle foodRect = new Rectangle((w - 560) / 2, (h - 400) / 2 - 40, 560, 400);
				KioskSkin.DrawFoodContactShadow(g, w / 2, foodRect.Bottom + 12, 480, 36);

				if (realImage != null)
				{
					float scale = Math.Min((float)foodRect.Width / realImage.Width, (float)foodRect.Height / realImage.Height);
					int drawW = (int)(realImage.Width * scale);
					int drawH = (int)(realImage.Height * scale);
					int drawX = foodRect.X + (foodRect.Width - drawW) / 2;
					int drawY = foodRect.Y + (foodRect.Height - drawH) / 2;
					g.DrawImage(realImage, new Rectangle(drawX, drawY, drawW, drawH));
				}
				else
				{
					ProductCardPanel.DrawFoodVectorIllustration(g, foodRect, prod.Category, prod.Name, true);
				}

			int cardW = 520;
			int cardH = 92;
			int cardX = (w - cardW) / 2;
			int cardY = foodRect.Bottom + 45;

				using (GraphicsPath cardPath = KioskSkin.RoundRect(new Rectangle(cardX, cardY, cardW, cardH), 22))
				{
					using (SolidBrush cardBg = new SolidBrush(Color.FromArgb(220, 255, 255, 255)))
					{
						g.FillPath(cardBg, cardPath);
					}
					using (Pen borderPen = new Pen(Color.FromArgb(160, 218, 225, 233), 1.5f))
					{
						g.DrawPath(borderPen, cardPath);
					}
				}

				var pastel = KioskSkin.GetCategoryPastel(prod.Category);
				using (GraphicsPath catPill = KioskSkin.RoundRect(new Rectangle(cardX + 24, cardY + 20, 120, 26), 8))
				{
					using (SolidBrush catBg = new SolidBrush(pastel.Bg))
					{
						g.FillPath(catBg, catPill);
					}
				}
				using (Font catFont = new Font("Segoe UI Bold", 9.5f))
				using (SolidBrush catText = new SolidBrush(pastel.Fore))
				{
					StringFormat sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
					g.DrawString((prod.Category ?? "MENU").ToUpperInvariant(), catFont, catText, new Rectangle(cardX + 24, cardY + 20, 120, 26), sf);
				}

				using (Font titleFont = new Font("Segoe UI Bold", 16f))
				using (SolidBrush titleBrush = new SolidBrush(Color.FromArgb(26, 32, 44)))
				{
					g.DrawString(prod.Name, titleFont, titleBrush, new PointF(cardX + 24, cardY + 52));
				}

				using (Font priceFont = new Font("Segoe UI Bold", 20f))
				using (SolidBrush priceBrush = new SolidBrush(Color.FromArgb(45, 96, 255)))
				{
					StringFormat sfRight = new StringFormat { Alignment = StringAlignment.Far, LineAlignment = StringAlignment.Center };
					g.DrawString(KioskLang.FormatPrice(prod.Price, currency), priceFont, priceBrush, new Rectangle(cardX + cardW - 170, cardY + 20, 150, 52), sfRight);
				}
			}

			return bmp;
		}

		private static Bitmap GenerateFallbackPromoImage(int w, int h)
		{
			Bitmap bitmap = new Bitmap(w, h);
			using Graphics graphics = Graphics.FromImage(bitmap);
			graphics.SmoothingMode = SmoothingMode.AntiAlias;
			using (LinearGradientBrush brush = new LinearGradientBrush(new Point(0, 0), new Point(w, h), Color.FromArgb(246, 238, 226), Color.FromArgb(226, 210, 190)))
			{
				graphics.FillRectangle(brush, 0, 0, w, h);
			}
			using (SolidBrush brush2 = new SolidBrush(Color.FromArgb(40, 255, 210, 120)))
			{
				graphics.FillEllipse(brush2, w / 5, h / 6, Math.Min(650, w - 40), Math.Min(650, h - 40));
			}
			Rectangle r = new Rectangle((w - Math.Min(560, w - 40)) / 2, (h - Math.Min(380, h - 40)) / 2 - 20, Math.Min(560, w - 40), Math.Min(380, h - 40));
			KioskSkin.DrawFoodContactShadow(graphics, w / 2, r.Bottom + 12, (int)(r.Width * 0.85f), 36);
			ProductCardPanel.DrawFoodVectorIllustration(graphics, r, "Pastries", "Croissant", isHovered: true);
			return bitmap;
		}

		protected override void OnPaint(PaintEventArgs e)
		{
			base.OnPaint(e);
			Graphics graphics = e.Graphics;
			graphics.SmoothingMode = SmoothingMode.AntiAlias;
			int num = Width;
			int num2 = Height;
			Image image = ((promoImages.Count > 0 && currentSlideIndex < promoImages.Count) ? promoImages[currentSlideIndex] : null);
			if (image != null)
			{
				float num3 = Math.Max((float)num / (float)image.Width, (float)num2 / (float)image.Height);
				int num4 = (int)((float)image.Width * num3);
				int num5 = (int)((float)image.Height * num3);
				int num6 = (num - num4) / 2;
				int num7 = (num2 - num5) / 2;
				graphics.DrawImage(image, new Rectangle(num6, num7, num4, num5));
			}
			else
			{
				using SolidBrush brush = new SolidBrush(KioskSkin.Bg);
				graphics.FillRectangle(brush, 0, 0, num, num2);
			}
			using (LinearGradientBrush brush2 = new LinearGradientBrush(new Point(0, 0), new Point(0, 140), Color.FromArgb(KioskSkin.IsDark ? 160 : 120, KioskSkin.Bg), Color.FromArgb(0, KioskSkin.Bg)))
			{
				graphics.FillRectangle(brush2, 0, 0, num, 140);
			}
			using (LinearGradientBrush brush3 = new LinearGradientBrush(new Point(0, num2 - 220), new Point(0, num2), Color.FromArgb(0, KioskSkin.Bg), Color.FromArgb(KioskSkin.IsDark ? 180 : 140, KioskSkin.Bg)))
			{
				graphics.FillRectangle(brush3, 0, num2 - 220, num, 220);
			}
			string s = "◈  " + mainForm.RestaurantName;
			using Font font = new Font("Segoe UI Bold", 13.5f);
			Rectangle rectangle = new Rectangle(36, 30, (int)graphics.MeasureString(s, font).Width + 32, 44);
			using (GraphicsPath path = KioskSkin.RoundRect(rectangle, 22))
			{
				using SolidBrush brush4 = new SolidBrush(Color.FromArgb(190, 15, 20, 30));
				graphics.FillPath(brush4, path);
				using Pen pen = new Pen(Color.FromArgb(80, 255, 255, 255), 1.2f);
				graphics.DrawPath(pen, path);
			}
			using (SolidBrush brush5 = new SolidBrush(Color.White))
			{
				using StringFormat format = new StringFormat
				{
					Alignment = StringAlignment.Center,
					LineAlignment = StringAlignment.Center
				};
				graphics.DrawString(s, font, brush5, rectangle, format);
			}
			if (image == null)
			{
				int num8 = num2 / 2 - 35;
				int num9 = Math.Min(980, num - 80);
				int num10 = 224;
				int num11 = (num - num9) / 2;
				int num12 = num8 - 110;
				Rectangle r = new Rectangle(num11, num12, num9, num10);
				KioskSkin.DrawCardShadow(graphics, r, 26, 8);
				using (GraphicsPath path2 = KioskSkin.RoundRect(r, 26))
				{
					using SolidBrush brush6 = new SolidBrush(KioskSkin.IsDark ? Color.FromArgb(215, 15, 20, 30) : Color.FromArgb(210, 255, 255, 255));
					graphics.FillPath(brush6, path2);
					using Pen pen2 = new Pen(KioskSkin.IsDark ? Color.FromArgb(70, 56, 189, 248) : Color.FromArgb(170, 255, 255, 255), 1.5f);
					graphics.DrawPath(pen2, path2);
				}
				string text = "★  " + KioskLang.T("AttractSub");
				using Font font2 = new Font("Segoe UI Bold", 10.5f);
				using SolidBrush brush7 = new SolidBrush(KioskSkin.Gold);
				using StringFormat format2 = new StringFormat
				{
					Alignment = StringAlignment.Center
				};
				graphics.DrawString(text.ToUpperInvariant(), font2, brush7, new Rectangle(num11 + 20, num12 + 28, num9 - 40, 24), format2);
				string s2 = KioskLang.T("AttractTitle");
				using Font font3 = new Font("Segoe UI Bold", 26f);
				using SolidBrush brush8 = new SolidBrush(KioskSkin.Text);
				graphics.DrawString(s2, font3, brush8, new Rectangle(num11 + 20, num12 + 62, num9 - 40, 58), format2);
				string s3 = mainForm.RestaurantName + " \a " + ((KioskLang.Current == "fr") ? "Fraîcheur & Excellence Gourmande" : "Fresh, Handcrafted & Cooked to Order");
				using Font font4 = new Font("Segoe UI", 12.5f);
				using SolidBrush brush9 = new SolidBrush(KioskSkin.TextMuted);
				graphics.DrawString(s3, font4, brush9, new Rectangle(num11 + 20, num12 + 138, num9 - 40, 32), format2);
			}
			else
			{
				string s4 = "★  " + KioskLang.T("AttractSub").ToUpperInvariant() + " • " + KioskLang.T("AttractTitle");
				using Font font5 = new Font("Segoe UI Bold", 11.5f);
				int num13 = Math.Min((int)graphics.MeasureString(s4, font5).Width + 44, num - 60);
				int num14 = (num - num13) / 2;
				int num15 = num2 - (int)((double)num2 * 0.1) - 126;
				Rectangle rectangle2 = new Rectangle(num14, num15, num13, 42);
				using (GraphicsPath path3 = KioskSkin.RoundRect(rectangle2, 21))
				{
					using SolidBrush brush10 = new SolidBrush(Color.FromArgb(190, 15, 20, 30));
					graphics.FillPath(brush10, path3);
					using Pen pen3 = new Pen(Color.FromArgb(80, 255, 255, 255), 1.2f);
					graphics.DrawPath(pen3, path3);
				}
				using SolidBrush brush11 = new SolidBrush(Color.White);
				using StringFormat format3 = new StringFormat
				{
					Alignment = StringAlignment.Center,
					LineAlignment = StringAlignment.Center
				};
				graphics.DrawString(s4, font5, brush11, rectangle2, format3);
			}
			int num16 = Math.Min(520, num - 80);
			int num17 = 74;
			int num18 = (int)((float)num16 * pulseScale);
			int num19 = (int)((float)num17 * pulseScale);
			int num20 = (num - num18) / 2;
			int num21 = num2 - num19 - (int)((double)num2 * 0.1);
			Rectangle rectangle3 = new Rectangle(num20, num21, num18, num19);
			KioskSkin.DrawCardShadow(graphics, rectangle3, 37, 12);
			using (GraphicsPath path4 = KioskSkin.RoundRect(rectangle3, 37))
			{
				using SolidBrush brush12 = new SolidBrush(KioskSkin.Accent);
				graphics.FillPath(brush12, path4);
			}
			string s5 = KioskLang.T("TapToOrder");
			using Font font6 = new Font("Segoe UI Bold", 17f);
			using SolidBrush brush13 = new SolidBrush(KioskSkin.OnAccent);
			using StringFormat format4 = new StringFormat
			{
				Alignment = StringAlignment.Center,
				LineAlignment = StringAlignment.Center
			};
			graphics.DrawString(s5, font6, brush13, rectangle3, format4);
		}
	}
	internal sealed class ServiceChoiceCard : Panel
	{
		private bool isHovered;

		public string ServiceType { get; }

		public string Title { get; }

		public string Subtitle { get; }

		public string[] Highlights { get; }

		public string ButtonText { get; }

		public Action OnSelect { get; }

		public ServiceChoiceCard(string serviceType, string title, string subtitle, string[] highlights, string buttonText, Action onSelect)
		{
			ServiceType = serviceType;
			Title = title;
			Subtitle = subtitle;
			Highlights = highlights;
			ButtonText = buttonText;
			OnSelect = onSelect;
			DoubleBuffered = true;
			Cursor = Cursors.Hand;
			BackColor = Color.Transparent;
			MouseEnter += (object s, EventArgs e) =>
			{
				isHovered = true;
				Invalidate();
			};
			MouseLeave += (object s, EventArgs e) =>
			{
				isHovered = false;
				Invalidate();
			};
			Click += (object s, EventArgs e) =>
			{
				OnSelect();
			};
		}

		protected override void OnPaint(PaintEventArgs e)
		{
			base.OnPaint(e);
			Graphics graphics = e.Graphics;
			graphics.SmoothingMode = SmoothingMode.AntiAlias;
			int num = Width;
			int num2 = Height;
			Rectangle r = new Rectangle(0, 0, num - 1, num2 - 1);
			KioskSkin.DrawCardShadow(graphics, r, 22, isHovered ? 8 : 4);
			using (GraphicsPath path = KioskSkin.RoundRect(r, 22))
			{
				using SolidBrush brush = new SolidBrush(isHovered ? KioskSkin.CardHover : KioskSkin.CardBg);
				graphics.FillPath(brush, path);
				using Pen pen = new Pen(isHovered ? KioskSkin.Accent : KioskSkin.CardBorder, isHovered ? 2f : 1f);
				graphics.DrawPath(pen, path);
			}
			int num3 = Math.Min(150, (int)((double)num2 * 0.38));
			new Rectangle(20, 20, num - 40, num3);
			Color color = ((ServiceType == "DineIn") ? KioskSkin.Gold : KioskSkin.Accent);
			int num4 = Math.Min(110, num3 - 10);
			int num5 = (num - num4) / 2;
			int num6 = 20 + (num3 - num4) / 2;
			using (SolidBrush brush2 = new SolidBrush(Color.FromArgb(isHovered ? 45 : 25, color)))
			{
				graphics.FillEllipse(brush2, num5, num6, num4, num4);
			}
			KioskSkin.DrawFoodContactShadow(graphics, num / 2, num6 + num4 - 8, (int)((double)num4 * 0.8), 16);
			if (ServiceType == "DineIn")
			{
				int num7 = num / 2;
				int num8 = num6 + num4 / 2 - 5;
				int num9 = (int)((double)num4 * 0.65);
				int num10 = (int)((double)num4 * 0.4);
				using GraphicsPath graphicsPath = new GraphicsPath();
				graphicsPath.AddArc(num7 - num9 / 2, num8 - num10, num9, num10 * 2, 180f, 180f);
				graphicsPath.CloseFigure();
				using LinearGradientBrush brush3 = new LinearGradientBrush(new Point(num7, num8 - num10), new Point(num7, num8), Color.FromArgb(250, 230, 180), Color.FromArgb(190, 140, 60));
				graphics.FillPath(brush3, graphicsPath);
				using SolidBrush brush4 = new SolidBrush(KioskSkin.Gold);
				graphics.FillEllipse(brush4, num7 - 6, num8 - num10 - 7, 12, 8);
				using Pen pen2 = new Pen(Color.FromArgb(210, 180, 110), 3f);
				graphics.DrawLine(pen2, num7 - num9 / 2 - 8, num8 + 2, num7 + num9 / 2 + 8, num8 + 2);
			}
			else
			{
				int num11 = num / 2;
				int num12 = num6 + num4 / 2 - 5;
				int num13 = (int)((double)num4 * 0.48);
				int num14 = (int)((double)num4 * 0.54);
				Rectangle r2 = new Rectangle(num11 - num13 / 2, num12 - num14 / 2, num13, num14);
				using LinearGradientBrush brush5 = new LinearGradientBrush(new Point(r2.X, r2.Y), new Point(r2.Right, r2.Bottom), Color.FromArgb(220, 140, 75), Color.FromArgb(170, 95, 45));
				using GraphicsPath path2 = KioskSkin.RoundRect(r2, 6);
				graphics.FillPath(brush5, path2);
				using Pen pen3 = new Pen(Color.FromArgb(140, 75, 30), 2.5f);
				graphics.DrawArc(pen3, num11 - 10, r2.Y - 10, 20, 16, 180, 180);
				int num15 = (int)((double)num13 * 0.38);
				int num16 = (int)((double)num14 * 0.55);
				Rectangle rect = new Rectangle(num11 + num13 / 2 - 4, r2.Bottom - num16, num15, num16);
				using SolidBrush brush6 = new SolidBrush(KioskSkin.Accent);
				graphics.FillRectangle(brush6, rect);
				using Pen pen4 = new Pen(Color.FromArgb(255, 90, 90), 2.5f);
				graphics.DrawLine(pen4, rect.X + num15 / 2, rect.Y, rect.X + num15 / 2 + 6, rect.Y - 12);
			}
			int num17 = 20 + num3 + 10;
			using Font font = new Font("Segoe UI Bold", 18f);
			using SolidBrush brush7 = new SolidBrush(KioskSkin.Text);
			using StringFormat format = new StringFormat
			{
				Alignment = StringAlignment.Center
			};
			graphics.DrawString(Title, font, brush7, new Rectangle(14, num17, num - 28, 36), format);
			num17 += 38;
			using Font font2 = new Font("Segoe UI", 10.5f);
			using SolidBrush brush8 = new SolidBrush(KioskSkin.TextMuted);
			graphics.DrawString(Subtitle, font2, brush8, new Rectangle(16, num17, num - 32, 42), format);
			num17 += 46;
			int num18 = 58;
			int num19 = 18;
			if (Highlights != null && Highlights.Length != 0 && num2 >= 380)
			{
				using Font font3 = new Font("Segoe UI Semibold", 10f);
				using SolidBrush brush9 = new SolidBrush(KioskSkin.Text);
				using SolidBrush brush10 = new SolidBrush(color);
				int num20 = num17 + 6;
				string[] highlights = Highlights;
				foreach (string s in highlights)
				{
					if (num20 + 22 > num2 - num18 - num19 - 10)
					{
						break;
					}
					int num21 = (num - (int)graphics.MeasureString(s, font3).Width) / 2;
					graphics.FillEllipse(brush10, num21 - 12, num20 + 7, 5, 5);
					graphics.DrawString(s, font3, brush9, num21, num20);
					num20 += 24;
				}
			}
			int num22 = num2 - num18 - num19;
			Rectangle rectangle = new Rectangle(num19, num22, num - num19 * 2, num18);
			using (GraphicsPath path3 = KioskSkin.RoundRect(rectangle, 16))
			{
				using SolidBrush brush11 = new SolidBrush(isHovered ? KioskSkin.AccentHover : KioskSkin.Accent);
				graphics.FillPath(brush11, path3);
			}
			using Font font4 = new Font("Segoe UI Bold", 12f);
			using SolidBrush brush12 = new SolidBrush(KioskSkin.OnAccent);
			using StringFormat format2 = new StringFormat
			{
				Alignment = StringAlignment.Center,
				LineAlignment = StringAlignment.Center
			};
			graphics.DrawString(ButtonText, font4, brush12, rectangle, format2);
		}
	}
	internal sealed class CategoryCardPanel : Panel
	{
		private readonly Action onSelect;

		private bool isHovered;

		public string CategoryName { get; }

		public string IconText { get; }

		public int ItemCount { get; }

		public bool IsActive { get; set; }

		public CategoryCardPanel(string name, string icon, int itemCount, bool isActive, Action onSelect)
		{
			CategoryName = name;
			IconText = icon;
			ItemCount = itemCount;
			IsActive = isActive;
			this.onSelect = onSelect;
			Size = new Size(130, 88);
			Margin = new Padding(0, 4, 12, 4);
			DoubleBuffered = true;
			Cursor = Cursors.Hand;
			BackColor = Color.Transparent;
			MouseEnter += (object s, EventArgs e) =>
			{
				isHovered = true;
				Invalidate();
			};
			MouseLeave += (object s, EventArgs e) =>
			{
				isHovered = false;
				Invalidate();
			};
			Click += (object s, EventArgs e) =>
			{
				onSelect();
			};
		}

		protected override void OnPaint(PaintEventArgs e)
		{
			base.OnPaint(e);
			Graphics graphics = e.Graphics;
			graphics.SmoothingMode = SmoothingMode.AntiAlias;
			using (GraphicsPath path = KioskSkin.RoundRect(new Rectangle(0, 0, Width - 1, Height - 1), 16))
			{
				Color color;
				if (IsActive)
				{
					color = (KioskSkin.IsDark ? Color.FromArgb(28, 42, 68) : Color.FromArgb(244, 248, 255));
				}
				else
				{
					color = (isHovered ? KioskSkin.CardHover : KioskSkin.CardBg);
				}
				using SolidBrush brush = new SolidBrush(color);
				graphics.FillPath(brush, path);
				Color color2;
				if (IsActive)
				{
					color2 = KioskSkin.Accent;
				}
				else
				{
					color2 = (isHovered ? Color.FromArgb(180, 200, 230) : KioskSkin.CardBorder);
				}
				using Pen pen = new Pen(color2, IsActive ? 2f : 1f);
				graphics.DrawPath(pen, path);
			}
			int num = 34;
			int num2 = (Width - num) / 2;
			int num3 = 8;
			Rectangle rectangle = new Rectangle(num2, num3, num, num);
			Color color3;
			if (IsActive)
			{
				color3 = KioskSkin.Accent;
			}
			else
			{
				color3 = (KioskSkin.IsDark ? Color.FromArgb(32, 40, 56) : Color.FromArgb(241, 245, 249));
			}
			using (SolidBrush brush2 = new SolidBrush(color3))
			{
				graphics.FillEllipse(brush2, rectangle);
			}
			DrawCategoryBadgeVector(color: (!IsActive) ? (KioskSkin.IsDark ? Color.FromArgb(148, 163, 184) : Color.FromArgb(100, 116, 139)) : Color.White, g: graphics, circle: rectangle, name: CategoryName);
			using StringFormat format = new StringFormat
			{
				Alignment = StringAlignment.Center,
				LineAlignment = StringAlignment.Center
			};
			using Font font = new Font("Segoe UI Bold", 10.5f);
			using SolidBrush brush3 = new SolidBrush(IsActive ? KioskSkin.Accent : KioskSkin.Text);
			graphics.DrawString(CategoryName, font, brush3, new Rectangle(4, 44, Width - 8, 20), format);
			string s = KioskLang.T("ItemsCount", ItemCount);
			using Font font2 = new Font("Segoe UI", 8.5f);
			using SolidBrush brush4 = new SolidBrush(IsActive ? Color.FromArgb(80, 130, 240) : KioskSkin.TextMuted);
			graphics.DrawString(s, font2, brush4, new Rectangle(4, 64, Width - 8, 18), format);
		}

		private static void DrawCategoryBadgeVector(Graphics g, Rectangle circle, string name, Color color)
		{
			int num = circle.X + circle.Width / 2;
			int num2 = circle.Y + circle.Height / 2;
			string text = (name ?? "").ToLowerInvariant();
			using Pen pen = new Pen(color, 1.8f)
			{
				StartCap = LineCap.Round,
				EndCap = LineCap.Round
			};
			using SolidBrush brush = new SolidBrush(color);
			if (text.Contains("all") || text.Contains("tout") || text.Contains("menu"))
			{
				g.DrawLine(pen, num - 8, num2 + 4, num + 8, num2 + 4);
				g.DrawArc(pen, num - 7, num2 - 6, 14, 10, 180, 180);
				g.FillEllipse(brush, num - 2, num2 - 8, 4, 3);
			}
			else if (text.Contains("bread") || text.Contains("pain") || text.Contains("boulange"))
			{
				g.DrawArc(pen, num - 7, num2 - 4, 14, 9, 180, 180);
				g.DrawLine(pen, num - 7, num2 + 1, num - 7, num2 + 4);
				g.DrawLine(pen, num + 7, num2 + 1, num + 7, num2 + 4);
				g.DrawLine(pen, num - 7, num2 + 4, num + 7, num2 + 4);
				g.DrawLine(pen, num - 3, num2 - 3, num - 1, num2 + 1);
				g.DrawLine(pen, num + 1, num2 - 3, num + 3, num2 + 1);
			}
			else if (text.Contains("cake") || text.Contains("gât") || text.Contains("dessert"))
			{
				Point[] points = new Point[4]
				{
					new Point(num - 7, num2 + 4),
					new Point(num + 7, num2 + 4),
					new Point(num + 4, num2 - 5),
					new Point(num - 7, num2 + 4)
				};
				g.DrawLines(pen, points);
				g.DrawLine(pen, num - 7, num2 + 1, num + 5, num2 + 1);
				g.FillEllipse(brush, num + 3, num2 - 7, 3, 3);
			}
			else if (text.Contains("donut") || text.Contains("doughnut"))
			{
				g.DrawEllipse(pen, num - 7, num2 - 7, 14, 14);
				g.DrawEllipse(pen, num - 3, num2 - 3, 6, 6);
			}
			else if (text.Contains("pastr") || text.Contains("croissant") || text.Contains("viennois"))
			{
				g.DrawArc(pen, num - 8, num2 - 5, 16, 10, 20, 140);
				g.DrawArc(pen, num - 7, num2 - 3, 14, 7, 200, 140);
				g.DrawLine(pen, num - 2, num2 - 3, num - 1, num2 + 2);
				g.DrawLine(pen, num + 2, num2 - 3, num + 1, num2 + 2);
			}
			else if (text.Contains("sandw") || text.Contains("crowich") || text.Contains("snack"))
			{
				Point[] points2 = new Point[4]
				{
					new Point(num - 7, num2 + 3),
					new Point(num + 7, num2 - 4),
					new Point(num + 7, num2 + 3),
					new Point(num - 7, num2 + 3)
				};
				g.DrawPolygon(pen, points2);
				g.DrawLine(pen, num - 5, num2 + 1, num + 5, num2 - 2);
			}
			else if (text.Contains("pizza"))
			{
				Point[] points3 = new Point[4]
				{
					new Point(num, num2 + 5),
					new Point(num - 7, num2 - 4),
					new Point(num + 7, num2 - 4),
					new Point(num, num2 + 5)
				};
				g.DrawPolygon(pen, points3);
				g.FillEllipse(brush, num - 2, num2 - 2, 3, 3);
				g.FillEllipse(brush, num + 1, num2 + 1, 2, 2);
			}
			else if (text.Contains("drink") || text.Contains("boisson") || text.Contains("café") || text.Contains("coffee") || text.Contains("tea"))
			{
				Point[] points4 = new Point[5]
				{
					new Point(num - 5, num2 - 2),
					new Point(num + 5, num2 - 2),
					new Point(num + 4, num2 + 5),
					new Point(num - 4, num2 + 5),
					new Point(num - 5, num2 - 2)
				};
				g.DrawPolygon(pen, points4);
				g.DrawLine(pen, num + 1, num2 - 2, num + 4, num2 - 7);
			}
			else
			{
				g.DrawLine(pen, num, num2 - 5, num, num2 + 5);
				g.DrawLine(pen, num - 5, num2, num + 5, num2);
			}
		}
	}
	internal sealed class ProductCardPanel : Panel
	{
		private readonly KioskMainForm mainForm;

		private bool isHovered;

		private Image? cachedImage;

		private readonly string currency;

		private readonly string serverUrl;

		public KioskProduct Product { get; }

		public ProductCardPanel(KioskProduct p, string currency, string serverUrl, string apiKey, KioskMainForm mainForm, int cardW = 325, int cardH = 340, int margin = 6)
		{
			ProductCardPanel productCardPanel = this;
			Product = p;
			this.currency = currency;
			this.serverUrl = serverUrl;
			this.mainForm = mainForm;
			Size = new Size(cardW, cardH);
			Margin = new Padding(margin);
			DoubleBuffered = true;
			Cursor = (p.OutOfStock ? Cursors.Default : Cursors.Hand);
			BackColor = Color.Transparent;
			cachedImage = CatalogImageCache.GetImage(p.Image, serverUrl, apiKey, () =>
			{
				if (productCardPanel.IsHandleCreated && !productCardPanel.IsDisposed)
				{
					try
					{
						productCardPanel.BeginInvoke((Action)(() =>
						{
							productCardPanel.cachedImage = CatalogImageCache.GetImage(p.Image, serverUrl, apiKey);
							productCardPanel.Invalidate();
						}));
					}
					catch
					{
					}
				}
			});
			MouseEnter += (object s, EventArgs e) =>
			{
				productCardPanel.isHovered = true;
				productCardPanel.Invalidate();
			};
			MouseLeave += (object s, EventArgs e) =>
			{
				productCardPanel.isHovered = false;
				productCardPanel.Invalidate();
			};
			HandleCreated += (object s, EventArgs e) =>
			{
				if (productCardPanel.cachedImage == null && !string.IsNullOrWhiteSpace(p.Image))
				{
					productCardPanel.cachedImage = CatalogImageCache.GetImage(p.Image, serverUrl, apiKey);
					if (productCardPanel.cachedImage != null)
					{
						productCardPanel.Invalidate();
					}
				}
			};
			Click += (object s, EventArgs e) =>
			{
				productCardPanel.OnCardClicked();
			};
		}

		public void SetCardSize(int cardW, int cardH, int margin = 6)
		{
			if (Width != cardW || Height != cardH)
			{
				Size = new Size(cardW, cardH);
			}
			if (Margin.Left != margin || Margin.Top != margin || Margin.Right != margin || Margin.Bottom != margin)
			{
				Margin = new Padding(margin);
			}
		}

		private void OnCardClicked()
		{
			if (!Product.OutOfStock)
			{
				mainForm.OpenProductDetailOrAdd(Product);
			}
		}

		protected override void OnPaint(PaintEventArgs e)
		{
			base.OnPaint(e);
			Graphics graphics = e.Graphics;
			graphics.SmoothingMode = SmoothingMode.AntiAlias;
			int num = Width;
			int num2 = Height;
			Rectangle rectangle = new Rectangle(0, 0, num - 1, num2 - 1);
			KioskSkin.DrawCardShadow(graphics, rectangle, 18, isHovered ? 6 : 3);
			using (GraphicsPath path = KioskSkin.RoundRect(rectangle, 18))
			{
				using SolidBrush brush = new SolidBrush(isHovered ? KioskSkin.CardHover : KioskSkin.CardBg);
				graphics.FillPath(brush, path);
				using Pen pen = new Pen(isHovered ? KioskSkin.Accent : KioskSkin.CardBorder, isHovered ? 1.5f : 1f);
				graphics.DrawPath(pen, path);
			}
			int foodH = Math.Max(100, (int)(num2 * 0.55f));
			KioskSkin.DrawFoodContactShadow(graphics, num / 2, foodH - 10, (int)((double)num * 0.65), 18);
			if (cachedImage != null)
			{
				int num4 = num - 24;
				int num5 = foodH - 20;
				float num6 = Math.Min((float)num4 / (float)cachedImage.Width, (float)num5 / (float)cachedImage.Height);
				int num7 = (int)((float)cachedImage.Width * num6);
				int num8 = (int)((float)cachedImage.Height * num6);
				int num9 = (num - num7) / 2;
				int num10 = 10 + (num5 - num8) / 2;
				graphics.DrawImage(cachedImage, new Rectangle(num9, num10, num7, num8));
			}
			else
			{
				Rectangle r = new Rectangle(14, 10, num - 28, foodH - 20);
				DrawFoodVectorIllustration(graphics, r, Product.Category, Product.Name, isHovered);
			}
			int titleTop = foodH + 4;
			float titleFontSize = num < 250 ? 10f : (num < 300 ? 11f : 12f);
			using Font font = new Font("Segoe UI Bold", titleFontSize);
			using SolidBrush brush2 = new SolidBrush(KioskSkin.Text);
			using StringFormat format = new StringFormat
			{
				Trimming = StringTrimming.EllipsisCharacter
			};
			int titleH = Math.Max(32, (int)(num2 * 0.16f));
			Rectangle rectangle2 = new Rectangle(12, titleTop, num - 24, titleH);
			graphics.DrawString(Product.Name, font, brush2, rectangle2, format);

			int bottomY = num2 - 34;
			string text = ((!string.IsNullOrWhiteSpace(Product.Category)) ? Product.Category : "General");
			(Color, Color) categoryPastel = KioskSkin.GetCategoryPastel(text);
			float badgeFontSize = num < 250 ? 7.5f : 8.5f;
			using Font font2 = new Font("Segoe UI Bold", badgeFontSize);
			int num13 = Math.Min(num / 2 - 14, (int)graphics.MeasureString(text, font2).Width + 14);
			int num14 = 22;
			Rectangle rectangle3 = new Rectangle(12, bottomY + 2, num13, num14);
			using (GraphicsPath path2 = KioskSkin.RoundRect(rectangle3, 6))
			{
				using SolidBrush brush3 = new SolidBrush(categoryPastel.Item1);
				graphics.FillPath(brush3, path2);
			}
			using (SolidBrush brush4 = new SolidBrush(categoryPastel.Item2))
			{
				using StringFormat format2 = new StringFormat
				{
					Alignment = StringAlignment.Center,
					LineAlignment = StringAlignment.Center,
					Trimming = StringTrimming.EllipsisCharacter
				};
				graphics.DrawString(text, font2, brush4, rectangle3, format2);
			}
			string s = KioskLang.FormatPrice(Product.Price, currency);
			float priceFontSize = num < 250 ? 11.5f : (num < 300 ? 13f : 14.5f);
			using Font font3 = new Font("Segoe UI Bold", priceFontSize);
			using SolidBrush brush5 = new SolidBrush(KioskSkin.Accent);
			using StringFormat format3 = new StringFormat
			{
				Alignment = StringAlignment.Far,
				LineAlignment = StringAlignment.Center
			};
			Rectangle rectangle4 = new Rectangle(12, bottomY - 2, num - 24, 28);
			graphics.DrawString(s, font3, brush5, rectangle4, format3);
			if (!Product.OutOfStock)
			{
				return;
			}
			using SolidBrush brush6 = new SolidBrush(Color.FromArgb(180, 240, 245, 250));
			using GraphicsPath path3 = KioskSkin.RoundRect(rectangle, 18);
			graphics.FillPath(brush6, path3);
			using Font font4 = new Font("Segoe UI Bold", 12f);
			using SolidBrush brush7 = new SolidBrush(KioskSkin.Danger);
			using StringFormat format4 = new StringFormat
			{
				Alignment = StringAlignment.Center,
				LineAlignment = StringAlignment.Center
			};
			graphics.DrawString("ÉPUISÉ", font4, brush7, rectangle, format4);
		}

		public static void DrawFoodVectorIllustration(Graphics g, Rectangle r, string category, string name, bool isHovered)
		{
			string text = (category + " " + name).ToLowerInvariant();
			int num = r.X + r.Width / 2;
			int num2 = r.Y + r.Height / 2 - 4;
			if (text.Contains("egg tart") || (text.Contains("tart") && !text.Contains("pop")))
			{
				int num3 = (int)((double)r.Width * 0.62);
				int num4 = (int)((double)r.Height * 0.46);
				using LinearGradientBrush brush = new LinearGradientBrush(new Point(num - num3 / 2, num2 - num4 / 2), new Point(num + num3 / 2, num2 + num4 / 2), Color.FromArgb(235, 170, 75), Color.FromArgb(170, 100, 30));
				Point[] points = new Point[4]
				{
					new Point(num - num3 / 2, num2 - num4 / 4),
					new Point(num - num3 / 3, num2 + num4 / 2),
					new Point(num + num3 / 3, num2 + num4 / 2),
					new Point(num + num3 / 2, num2 - num4 / 4)
				};
				g.FillPolygon(brush, points);
				using Pen pen = new Pen(Color.FromArgb(150, 85, 25), 2f);
				for (int i = 0; i <= 6; i++)
				{
					int num5 = num - num3 / 2 + i * num3 / 6;
					g.DrawArc(pen, num5 - 3, num2 - num4 / 4 - 3, 6, 6, 0, 180);
				}
				using LinearGradientBrush brush2 = new LinearGradientBrush(new Point(num, num2 - num4 / 3), new Point(num, num2 + num4 / 6), Color.FromArgb(255, 230, 80), Color.FromArgb(245, 180, 40));
				g.FillEllipse(brush2, num - num3 / 2 + 6, num2 - num4 / 3, num3 - 12, (int)((double)num4 * 0.55));
				using SolidBrush brush3 = new SolidBrush(Color.FromArgb(120, 60, 20));
				g.FillEllipse(brush3, num - 8, num2 - num4 / 6 + 2, 10, 6);
				g.FillEllipse(brush3, num + 6, num2 - num4 / 8, 7, 5);
				g.FillEllipse(brush3, num - 14, num2 - num4 / 8, 5, 4);
				using SolidBrush brush4 = new SolidBrush(Color.FromArgb(100, 255, 255, 255));
				g.FillEllipse(brush4, num - 6, num2 - num4 / 4 + 2, 12, 4);
				return;
			}
			if (text.Contains("croissant"))
			{
				int num6 = (int)((double)r.Width * 0.72);
				int num7 = (int)((double)r.Height * 0.52);
				using LinearGradientBrush brush5 = new LinearGradientBrush(new Point(num - num6 / 2, num2 - num7 / 2), new Point(num + num6 / 2, num2 + num7 / 2), Color.FromArgb(248, 185, 80), Color.FromArgb(180, 95, 25));
				using GraphicsPath graphicsPath = new GraphicsPath();
				graphicsPath.AddBezier(num - num6 / 2, num2 + num7 / 4, num - num6 / 4, num2 - num7 / 2 - 4, num + num6 / 4, num2 - num7 / 2 - 4, num + num6 / 2, num2 + num7 / 4);
				graphicsPath.AddBezier(num + num6 / 2, num2 + num7 / 4, num + num6 / 3, num2 + num7 / 8, num - num6 / 3, num2 + num7 / 8, num - num6 / 2, num2 + num7 / 4);
				graphicsPath.CloseFigure();
				g.FillPath(brush5, graphicsPath);
				using Pen pen2 = new Pen(Color.FromArgb(150, 75, 15), 2.5f);
				g.DrawArc(pen2, num - 16, num2 - num7 / 2 + 2, 32, num7 - 6, 25, 130);
				g.DrawArc(pen2, num - 34, num2 - num7 / 2 + 6, 30, num7 - 12, 35, 115);
				g.DrawArc(pen2, num + 4, num2 - num7 / 2 + 6, 30, num7 - 12, 30, 115);
				g.DrawArc(pen2, num - 48, num2 - num7 / 4 + 4, 22, num7 / 2, 45, 100);
				g.DrawArc(pen2, num + 26, num2 - num7 / 4 + 4, 22, num7 / 2, 35, 100);
				using SolidBrush brush6 = new SolidBrush(Color.FromArgb(90, 255, 255, 255));
				g.FillEllipse(brush6, num - 12, num2 - num7 / 4 + 2, 24, 7);
				g.FillEllipse(brush6, num - 28, num2 - num7 / 4 + 6, 12, 5);
				g.FillEllipse(brush6, num + 16, num2 - num7 / 4 + 6, 12, 5);
				return;
			}
			if (text.Contains("roll") || text.Contains("roulé"))
			{
				int num8 = (int)((double)r.Width * 0.65);
				int num9 = (int)((double)r.Height * 0.48);
				using LinearGradientBrush brush7 = new LinearGradientBrush(new Point(num - num8 / 2, num2), new Point(num + num8 / 2, num2), Color.FromArgb(245, 180, 75), Color.FromArgb(175, 95, 25));
				Point[] points2 = new Point[4]
				{
					new Point(num - num8 / 2, num2 + num9 / 3),
					new Point(num + num8 / 4, num2 - num9 / 2),
					new Point(num + num8 / 2, num2 - num9 / 3),
					new Point(num - num8 / 4, num2 + num9 / 2)
				};
				g.FillPolygon(brush7, points2);
				using Pen pen3 = new Pen(Color.FromArgb(145, 75, 20), 2.5f);
				g.DrawLine(pen3, num - num8 / 3, num2 + num9 / 3, num, num2 - num9 / 3);
				g.DrawLine(pen3, num - num8 / 6, num2 + num9 / 3, num + num8 / 6, num2 - num9 / 3);
				g.DrawLine(pen3, num, num2 + num9 / 4, num + num8 / 3, num2 - num9 / 4);
				using SolidBrush brush8 = new SolidBrush(Color.FromArgb(50, 25, 15));
				g.FillEllipse(brush8, num + num8 / 3 - 4, num2 - num9 / 2 + 2, 18, 14);
				g.FillEllipse(brush8, num - num8 / 2, num2 + num9 / 4, 16, 12);
				return;
			}
			if (text.Contains("crowich") || text.Contains("sandwich"))
			{
				int num10 = (int)((double)r.Width * 0.7);
				int num11 = (int)((double)r.Height * 0.52);
				using LinearGradientBrush brush9 = new LinearGradientBrush(new Point(num, num2 - num11 / 2), new Point(num, num2 - num11 / 8), Color.FromArgb(245, 180, 75), Color.FromArgb(195, 115, 35));
				using GraphicsPath graphicsPath2 = new GraphicsPath();
				graphicsPath2.AddArc(num - num10 / 2, num2 - num11 / 2, num10, (int)((double)num11 * 0.45), 180f, 180f);
				graphicsPath2.CloseFigure();
				g.FillPath(brush9, graphicsPath2);
				using SolidBrush brush10 = new SolidBrush(Color.FromArgb(80, 255, 255, 255));
				g.FillEllipse(brush10, num - num10 / 4, num2 - num11 / 2 + 4, num10 / 2, 6);
				using SolidBrush brush11 = new SolidBrush(Color.FromArgb(55, 175, 50));
				for (int j = 0; j < 7; j++)
				{
					int num12 = num - num10 / 2 + 4 + j * (num10 - 8) / 6;
					g.FillEllipse(brush11, num12 - 6, num2 - num11 / 10, 14, 10);
				}
				using SolidBrush brush12 = new SolidBrush(Color.FromArgb(225, 45, 40));
				g.FillRectangle(brush12, num - num10 / 2 + 10, num2 - 2, (num10 - 20) / 2 - 2, 6);
				g.FillRectangle(brush12, num + 4, num2 - 2, (num10 - 20) / 2 - 2, 6);
				using SolidBrush brush13 = new SolidBrush(Color.FromArgb(90, 48, 28));
				g.FillRectangle(brush13, num - num10 / 2 + 6, num2 + 4, num10 - 12, 9);
				using SolidBrush brush14 = new SolidBrush(Color.FromArgb(255, 190, 25));
				Point[] points3 = new Point[3]
				{
					new Point(num + 8, num2 + 8),
					new Point(num + 26, num2 + 8),
					new Point(num + 18, num2 + 18)
				};
				g.FillPolygon(brush14, points3);
				using LinearGradientBrush brush15 = new LinearGradientBrush(new Point(num, num2 + 12), new Point(num, num2 + num11 / 2), Color.FromArgb(200, 125, 45), Color.FromArgb(160, 90, 25));
				g.FillRectangle(brush15, num - num10 / 2 + 8, num2 + 13, num10 - 16, (int)((double)num11 * 0.22));
				return;
			}
			if (text.Contains("donut") || text.Contains("doughnut"))
			{
				int num13 = Math.Min((int)((double)r.Width * 0.65), (int)((double)r.Height * 0.65));
				int num14 = (int)((double)num13 * 0.36);
				using LinearGradientBrush brush16 = new LinearGradientBrush(new Point(num - num13 / 2, num2 - num13 / 2), new Point(num + num13 / 2, num2 + num13 / 2), Color.FromArgb(240, 175, 90), Color.FromArgb(175, 105, 35));
				using GraphicsPath graphicsPath3 = new GraphicsPath();
				graphicsPath3.AddEllipse(num - num13 / 2, num2 - num13 / 2, num13, num13);
				graphicsPath3.AddEllipse(num - num14 / 2, num2 - num14 / 2, num14, num14);
				g.FillPath(brush16, graphicsPath3);
				int num15 = (int)((double)num13 * 0.88);
				using LinearGradientBrush brush17 = new LinearGradientBrush(new Point(num - num15 / 2, num2 - num15 / 2), new Point(num + num15 / 2, num2 + num15 / 2), Color.FromArgb(90, 48, 25), Color.FromArgb(55, 28, 14));
				using GraphicsPath graphicsPath4 = new GraphicsPath();
				graphicsPath4.AddEllipse(num - num15 / 2, num2 - num15 / 2, num15, num15);
				graphicsPath4.AddEllipse(num - (int)((double)num14 * 1.1) / 2, num2 - (int)((double)num14 * 1.1) / 2, (int)((double)num14 * 1.1), (int)((double)num14 * 1.1));
				g.FillPath(brush17, graphicsPath4);
				using SolidBrush brush18 = new SolidBrush(Color.FromArgb(70, 255, 255, 255));
				g.FillEllipse(brush18, num - num13 / 3, num2 - num13 / 3, num13 / 4, 7);
				Color[] array = new Color[4]
				{
					Color.FromArgb(255, 215, 60),
					Color.FromArgb(255, 105, 180),
					Color.FromArgb(100, 200, 255),
					Color.FromArgb(255, 255, 255)
				};
				for (int k = 0; k < 14; k++)
				{
					double num16 = (double)k * 25.7 * Math.PI / 180.0;
					int num17 = (num13 + num14) / 4 + k % 3 * 2;
					int num18 = num + (int)(Math.Cos(num16) * (double)num17);
					int num19 = num2 + (int)(Math.Sin(num16) * (double)num17);
					using SolidBrush brush19 = new SolidBrush(array[k % array.Length]);
					g.FillEllipse(brush19, num18 - 2, num19 - 2, 4, 4);
				}
				return;
			}
			if (text.Contains("cheesecake") || text.Contains("black forest") || text.Contains("cake") || text.Contains("gât"))
			{
				bool flag = text.Contains("black forest") || text.Contains("forest");
				int num20 = (int)((double)r.Width * 0.65);
				int num21 = (int)((double)r.Height * 0.5);
				Point[] points4 = new Point[3]
				{
					new Point(num - num20 / 2, num2 + num21 / 3),
					new Point(num + num20 / 2, num2 + num21 / 6),
					new Point(num + num20 / 4, num2 - num21 / 2)
				};
				if (flag)
				{
					using (SolidBrush brush20 = new SolidBrush(Color.FromArgb(45, 25, 18)))
					{
						g.FillPolygon(brush20, points4);
						using SolidBrush brush21 = new SolidBrush(Color.FromArgb(250, 250, 250));
						g.FillPolygon(brush21, new Point[4]
						{
							new Point(num - num20 / 3, num2 + num21 / 8),
							new Point(num + num20 / 3, num2 + num21 / 16),
							new Point(num + num20 / 3, num2 + num21 / 16 + 5),
							new Point(num - num20 / 3, num2 + num21 / 8 + 5)
						});
						using SolidBrush brush22 = new SolidBrush(Color.FromArgb(210, 25, 45));
						g.FillEllipse(brush22, num + num20 / 6, num2 - num21 / 2 - 4, 12, 12);
						using Pen pen4 = new Pen(Color.FromArgb(80, 130, 40), 1.5f);
						g.DrawArc(pen4, num + num20 / 6 + 4, num2 - num21 / 2 - 12, 10, 12, 180, 120);
						return;
					}
				}
				using LinearGradientBrush brush23 = new LinearGradientBrush(new Point(num, num2 - num21 / 2), new Point(num, num2 + num21 / 3), Color.FromArgb(255, 248, 210), Color.FromArgb(245, 220, 150));
				g.FillPolygon(brush23, points4);
				using SolidBrush brush24 = new SolidBrush(Color.FromArgb(170, 100, 40));
				g.FillPolygon(brush24, new Point[4]
				{
					new Point(num - num20 / 2, num2 + num21 / 3),
					new Point(num + num20 / 2, num2 + num21 / 6),
					new Point(num + num20 / 2, num2 + num21 / 6 + 7),
					new Point(num - num20 / 2, num2 + num21 / 3 + 7)
				});
				using Pen pen5 = new Pen(Color.FromArgb(220, 160, 70), 3f);
				g.DrawLine(pen5, num - num20 / 2, num2 + num21 / 3, num + num20 / 4, num2 - num21 / 2);
				using SolidBrush brush25 = new SolidBrush(Color.FromArgb(215, 30, 60));
				g.FillEllipse(brush25, num - 6, num2 - 2, 7, 7);
				g.FillEllipse(brush25, num + 8, num2 + 4, 6, 6);
				return;
			}
			if (text.Contains("sourdough") || text.Contains("bread") || text.Contains("pain"))
			{
				int num22 = (int)((double)r.Width * 0.7);
				int num23 = (int)((double)r.Height * 0.44);
				using LinearGradientBrush brush26 = new LinearGradientBrush(new Point(num, num2 - num23 / 2), new Point(num, num2 + num23 / 2), Color.FromArgb(205, 125, 45), Color.FromArgb(135, 75, 25));
				using GraphicsPath path = KioskSkin.RoundRect(new Rectangle(num - num22 / 2, num2 - num23 / 2, num22, num23), 22);
				g.FillPath(brush26, path);
				using Pen pen6 = new Pen(Color.FromArgb(248, 240, 225), 2.5f);
				using Pen pen7 = new Pen(Color.FromArgb(100, 50, 15), 1.5f);
				int num24 = num - num22 / 3;
				int num25 = num - num22 / 9;
				int num26 = num + num22 / 9;
				int num27 = num + num22 / 3;
				g.DrawArc(pen6, num24 - 8, num2 - num23 / 2 + 4, 16, num23 - 12, 190, 160);
				g.DrawArc(pen7, num24 - 7, num2 - num23 / 2 + 6, 14, num23 - 16, 190, 160);
				g.DrawArc(pen6, num25 - 8, num2 - num23 / 2 + 3, 16, num23 - 10, 190, 160);
				g.DrawArc(pen7, num25 - 7, num2 - num23 / 2 + 5, 14, num23 - 14, 190, 160);
				g.DrawArc(pen6, num26 - 8, num2 - num23 / 2 + 3, 16, num23 - 10, 190, 160);
				g.DrawArc(pen7, num26 - 7, num2 - num23 / 2 + 5, 14, num23 - 14, 190, 160);
				g.DrawArc(pen6, num27 - 8, num2 - num23 / 2 + 4, 16, num23 - 12, 190, 160);
				g.DrawArc(pen7, num27 - 7, num2 - num23 / 2 + 6, 14, num23 - 16, 190, 160);
				return;
			}
			if (text.Contains("burger") || text.Contains("chicken"))
			{
				int num28 = (int)((double)r.Width * 0.7);
				int num29 = (int)((double)r.Height * 0.52);
				using LinearGradientBrush brush27 = new LinearGradientBrush(new Point(num, num2 - num29 / 2), new Point(num, num2 - num29 / 8), Color.FromArgb(240, 175, 75), Color.FromArgb(185, 110, 35));
				using GraphicsPath graphicsPath5 = new GraphicsPath();
				graphicsPath5.AddArc(num - num28 / 2, num2 - num29 / 2, num28, (int)((double)num29 * 0.52), 180f, 180f);
				graphicsPath5.CloseFigure();
				g.FillPath(brush27, graphicsPath5);
				using SolidBrush brush28 = new SolidBrush(Color.FromArgb(250, 245, 235));
				g.FillEllipse(brush28, num - 18, num2 - num29 / 3, 4, 2);
				g.FillEllipse(brush28, num + 12, num2 - num29 / 3 + 2, 4, 2);
				g.FillEllipse(brush28, num - 4, num2 - num29 / 2 + 6, 4, 2);
				using SolidBrush brush29 = new SolidBrush(Color.FromArgb(50, 180, 50));
				for (int l = 0; l < 6; l++)
				{
					int num30 = num - num28 / 2 + 6 + l * (num28 - 12) / 5;
					g.FillEllipse(brush29, num30 - 6, num2 - num29 / 12, 14, 8);
				}
				using SolidBrush brush30 = new SolidBrush(Color.FromArgb(225, 45, 40));
				g.FillRectangle(brush30, num - num28 / 2 + 8, num2, num28 - 16, 6);
				using SolidBrush brush31 = new SolidBrush(text.Contains("chicken") ? Color.FromArgb(215, 140, 40) : Color.FromArgb(85, 45, 25));
				g.FillRectangle(brush31, num - num28 / 2 + 4, num2 + 6, num28 - 8, 10);
				using SolidBrush brush32 = new SolidBrush(Color.FromArgb(255, 190, 20));
				Point[] points5 = new Point[3]
				{
					new Point(num - 8, num2 + 6),
					new Point(num + 8, num2 + 6),
					new Point(num, num2 + 16)
				};
				g.FillPolygon(brush32, points5);
				using SolidBrush brush33 = new SolidBrush(Color.FromArgb(195, 120, 45));
				g.FillRectangle(brush33, num - num28 / 2 + 8, num2 + 16, num28 - 16, (int)((double)num29 * 0.2));
				return;
			}
			if (text.Contains("pizza"))
			{
				int num31 = (int)((double)r.Width * 0.65);
				int num32 = (int)((double)r.Height * 0.55);
				Point[] points6 = new Point[3]
				{
					new Point(num, num2 + num32 / 2),
					new Point(num - num31 / 2, num2 - num32 / 2 + 8),
					new Point(num + num31 / 2, num2 - num32 / 2 + 8)
				};
				using LinearGradientBrush brush34 = new LinearGradientBrush(new Point(num, num2 - num32 / 2), new Point(num, num2 + num32 / 2), Color.FromArgb(255, 215, 60), Color.FromArgb(240, 160, 30));
				g.FillPolygon(brush34, points6);
				using LinearGradientBrush brush35 = new LinearGradientBrush(new Point(num, num2 - num32 / 2), new Point(num, num2 - num32 / 2 + 10), Color.FromArgb(210, 135, 55), Color.FromArgb(160, 90, 30));
				using GraphicsPath path2 = KioskSkin.RoundRect(new Rectangle(num - num31 / 2 - 2, num2 - num32 / 2, num31 + 4, 12), 6);
				g.FillPath(brush35, path2);
				using SolidBrush brush36 = new SolidBrush(Color.FromArgb(185, 35, 25));
				g.FillEllipse(brush36, num - 10, num2 - num32 / 6, 12, 12);
				g.FillEllipse(brush36, num + 4, num2 - num32 / 4, 11, 11);
				g.FillEllipse(brush36, num - 4, num2 + num32 / 8, 9, 9);
				return;
			}
			if (text.Contains("lemonade") || text.Contains("espresso") || text.Contains("drink") || text.Contains("boisson") || text.Contains("café"))
			{
				int num33 = (int)((double)r.Width * 0.48);
				int num34 = (int)((double)r.Height * 0.56);
				Color color = ((text.Contains("espresso") || text.Contains("café")) ? Color.FromArgb(70, 38, 22) : Color.FromArgb(250, 210, 50));
				using LinearGradientBrush brush37 = new LinearGradientBrush(new Point(num, num2 - num34 / 2), new Point(num, num2 + num34 / 2), Color.FromArgb(40, 120, 240, 255), Color.FromArgb(70, 45, 96, 255));
				Point[] points7 = new Point[4]
				{
					new Point(num - num33 / 2, num2 - num34 / 2 + 10),
					new Point(num + num33 / 2, num2 - num34 / 2 + 10),
					new Point(num + num33 / 2 - 8, num2 + num34 / 2),
					new Point(num - num33 / 2 + 8, num2 + num34 / 2)
				};
				g.FillPolygon(brush37, points7);
				using LinearGradientBrush brush38 = new LinearGradientBrush(new Point(num, num2 - num34 / 4), new Point(num, num2 + num34 / 2), color, Color.FromArgb(210, Math.Max(0, color.R / 2), Math.Max(0, color.G / 2), Math.Max(0, color.B / 2)));
				Point[] points8 = new Point[4]
				{
					new Point(num - num33 / 2 + 4, num2 - num34 / 4),
					new Point(num + num33 / 2 - 4, num2 - num34 / 4),
					new Point(num + num33 / 2 - 8, num2 + num34 / 2 - 2),
					new Point(num - num33 / 2 + 8, num2 + num34 / 2 - 2)
				};
				g.FillPolygon(brush38, points8);
				using Pen pen8 = new Pen(Color.FromArgb(235, 60, 50), 3.5f)
				{
					StartCap = LineCap.Round,
					EndCap = LineCap.Round
				};
				g.DrawLine(pen8, num + 4, num2 + num34 / 4, num + 18, num2 - num34 / 2 - 6);
				if (text.Contains("espresso") || text.Contains("café"))
				{
					return;
				}
				using SolidBrush brush39 = new SolidBrush(Color.FromArgb(255, 230, 40));
				using Pen pen9 = new Pen(Color.FromArgb(230, 190, 20), 2f);
				g.FillEllipse(brush39, num - num33 / 2 - 4, num2 - num34 / 2 + 2, 18, 18);
				g.DrawEllipse(pen9, num - num33 / 2 - 4, num2 - num34 / 2 + 2, 18, 18);
				return;
			}
			if (text.Contains("bowl") || text.Contains("salad") || text.Contains("salade"))
			{
				int num35 = (int)((double)r.Width * 0.65);
				int num36 = (int)((double)r.Height * 0.44);
				using LinearGradientBrush brush40 = new LinearGradientBrush(new Point(num, num2), new Point(num, num2 + num36 / 2), Color.FromArgb(250, 250, 252), Color.FromArgb(220, 226, 235));
				using GraphicsPath graphicsPath6 = new GraphicsPath();
				graphicsPath6.AddArc(num - num35 / 2, num2 - num36 / 4, num35, num36, 0f, 180f);
				graphicsPath6.CloseFigure();
				g.FillPath(brush40, graphicsPath6);
				using SolidBrush brush41 = new SolidBrush(Color.FromArgb(60, 185, 60));
				for (int m = 0; m < 8; m++)
				{
					int num37 = num - num35 / 2 + 8 + m * (num35 - 16) / 7;
					g.FillEllipse(brush41, num37 - 6, num2 - num36 / 4 - 2, 14, 10);
				}
				using SolidBrush brush42 = new SolidBrush(Color.FromArgb(235, 45, 35));
				g.FillEllipse(brush42, num - 12, num2 - num36 / 6, 8, 8);
				g.FillEllipse(brush42, num + 8, num2 - num36 / 5, 8, 8);
				using SolidBrush brush43 = new SolidBrush(Color.FromArgb(170, 205, 75));
				g.FillEllipse(brush43, num - 2, num2 - num36 / 8, 12, 6);
				return;
			}
			if (text.Contains("fries") || text.Contains("frites"))
			{
				int num38 = (int)((double)r.Width * 0.55);
				int num39 = (int)((double)r.Height * 0.54);
				using Pen pen10 = new Pen(Color.FromArgb(245, 195, 60), 4f)
				{
					StartCap = LineCap.Square,
					EndCap = LineCap.Square
				};
				using Pen pen11 = new Pen(Color.FromArgb(190, 140, 30), 4f);
				g.DrawLine(pen10, num - 14, num2 + num39 / 4, num - 18, num2 - num39 / 2 + 4);
				g.DrawLine(pen10, num - 6, num2 + num39 / 4, num - 8, num2 - num39 / 2);
				g.DrawLine(pen11, num + 2, num2 + num39 / 4, num + 2, num2 - num39 / 2 + 2);
				g.DrawLine(pen10, num + 10, num2 + num39 / 4, num + 12, num2 - num39 / 2 + 6);
				g.DrawLine(pen10, num + 18, num2 + num39 / 4, num + 22, num2 - num39 / 3);
				using LinearGradientBrush brush44 = new LinearGradientBrush(new Point(num - num38 / 2, num2), new Point(num + num38 / 2, num2), Color.FromArgb(230, 45, 35), Color.FromArgb(180, 25, 20));
				Point[] points9 = new Point[4]
				{
					new Point(num - num38 / 2, num2 - num39 / 8),
					new Point(num + num38 / 2, num2 - num39 / 8),
					new Point(num + num38 / 3, num2 + num39 / 2),
					new Point(num - num38 / 3, num2 + num39 / 2)
				};
				g.FillPolygon(brush44, points9);
				return;
			}
			int num40 = (int)((double)r.Width * 0.6);
			int num41 = (int)((double)r.Height * 0.44);
			using LinearGradientBrush brush45 = new LinearGradientBrush(new Point(num, num2 - num41 / 2), new Point(num, num2 + num41 / 2), Color.FromArgb(245, 205, 120), Color.FromArgb(195, 145, 55));
			using GraphicsPath graphicsPath7 = new GraphicsPath();
			graphicsPath7.AddArc(num - num40 / 2, num2 - num41 / 2, num40, num41, 180f, 180f);
			graphicsPath7.CloseFigure();
			g.FillPath(brush45, graphicsPath7);
			using Pen pen12 = new Pen(Color.FromArgb(160, 110, 35), 2.5f);
			g.DrawLine(pen12, num - num40 / 2 - 4, num2 + num41 / 2 - 2, num + num40 / 2 + 4, num2 + num41 / 2 - 2);
			using SolidBrush brush46 = new SolidBrush(Color.FromArgb(215, 165, 70));
			g.FillEllipse(brush46, num - 5, num2 - num41 / 2 - 4, 10, 8);
		}
	}
	internal sealed class UpsellDialog : Form
	{
		private readonly string currency;

		private readonly decimal baseTotal;

		private readonly Button proceedBtn;

		public List<KioskProduct> SelectedUpsells { get; } = new List<KioskProduct>();

		public UpsellDialog(string currency, decimal currentCartTotal)
		{
			UpsellDialog upsellDialog = this;
			this.currency = currency;
			baseTotal = currentCartTotal;
			FormBorderStyle = FormBorderStyle.None;
			TopMost = true;
			StartPosition = FormStartPosition.CenterParent;
			Size = new Size(860, 580);
			BackColor = KioskSkin.CardBg;
			DoubleBuffered = true;
			Paint += (object s, PaintEventArgs e) =>
			{
				Graphics graphics = e.Graphics;
				graphics.SmoothingMode = SmoothingMode.AntiAlias;
				using SolidBrush brush = new SolidBrush(KioskSkin.CardBg);
				graphics.FillRectangle(brush, 0, 0, upsellDialog.Width, upsellDialog.Height);
				using Pen pen = new Pen(KioskSkin.Accent, 2f);
				graphics.DrawRectangle(pen, 0, 0, upsellDialog.Width - 1, upsellDialog.Height - 1);
			};
			Label value = new Label
			{
				Text = KioskLang.T("UpsellTag"),
				Font = new Font("Segoe UI Bold", 10f),
				ForeColor = KioskSkin.Gold,
				TextAlign = ContentAlignment.MiddleCenter,
				Location = new Point(20, 20),
				Size = new Size(Width - 40, 24)
			};
			Controls.Add(value);
			Label value2 = new Label
			{
				Text = KioskLang.T("UpsellTitle"),
				Font = new Font("Segoe UI Bold", 18f),
				ForeColor = KioskSkin.Text,
				TextAlign = ContentAlignment.MiddleCenter,
				Location = new Point(20, 48),
				Size = new Size(Width - 40, 38)
			};
			Controls.Add(value2);
			Label value3 = new Label
			{
				Text = KioskLang.T("UpsellSubtitle"),
				Font = new Font("Segoe UI", 11f),
				ForeColor = KioskSkin.TextMuted,
				TextAlign = ContentAlignment.MiddleCenter,
				Location = new Point(20, 88),
				Size = new Size(Width - 40, 26)
			};
			Controls.Add(value3);
			(string, string, decimal, string)[] array = new (string, string, decimal, string)[4]
			{
				("UpsellFries", "UpsellFriesDesc", 2.50m, "Sides"),
				("UpsellDrink", "UpsellDrinkDesc", 2.80m, "Drinks"),
				("UpsellDessert", "UpsellDessertDesc", 3.50m, "Desserts"),
				("UpsellCoffee", "UpsellCoffeeDesc", 1.80m, "Drinks")
			};
			int num = 190;
			int num2 = 290;
			int num3 = 16;
			int num4 = array.Length * num + (array.Length - 1) * num3;
			int num5 = (Width - num4) / 2;
			int num6 = 126;
			for (int num7 = 0; num7 < array.Length; num7++)
			{
				(string NameKey, string DescKey, decimal Price, string Cat) def = array[num7];
				KioskProduct prod = new KioskProduct
				{
					Name = KioskLang.T(def.NameKey),
					Category = def.Cat,
					Price = def.Price,
					Color = "#2D60FF",
					Family = "Upsell"
				};
				Panel card = new Panel
				{
					Location = new Point(num5 + num7 * (num + num3), num6),
					Size = new Size(num, num2),
					BackColor = KioskSkin.CardBg,
					Cursor = Cursors.Hand
				};
				bool isSelected = false;
				card.Paint += (object s, PaintEventArgs pe) =>
				{
					Graphics graphics = pe.Graphics;
					graphics.SmoothingMode = SmoothingMode.AntiAlias;
					using SolidBrush brush = new SolidBrush(isSelected ? KioskSkin.CardHover : KioskSkin.CardBg);
					graphics.FillRectangle(brush, 0, 0, card.Width, card.Height);
					KioskSkin.DrawFoodContactShadow(graphics, card.Width / 2, 105, (int)((double)card.Width * 0.65), 18);
					Rectangle r = new Rectangle(12, 10, card.Width - 24, 95);
					ProductCardPanel.DrawFoodVectorIllustration(graphics, r, prod.Category, prod.Name, isSelected);
					using Font font = new Font("Segoe UI Bold", 11.5f);
					using SolidBrush brush2 = new SolidBrush(KioskSkin.Text);
					using StringFormat format = new StringFormat
					{
						Alignment = StringAlignment.Center
					};
					graphics.DrawString(prod.Name, font, brush2, new Rectangle(6, 126, card.Width - 12, 42), format);
					using Font font2 = new Font("Segoe UI", 9f);
					using SolidBrush brush3 = new SolidBrush(KioskSkin.TextMuted);
					graphics.DrawString(KioskLang.T(def.DescKey), font2, brush3, new Rectangle(8, 168, card.Width - 16, 48), format);
					using Pen pen = new Pen(isSelected ? KioskSkin.Accent : KioskSkin.CardBorder, isSelected ? 2f : 1f);
					graphics.DrawRectangle(pen, 0, 0, card.Width - 1, card.Height - 1);
				};
				Button addPill = new Button
				{
					Text = "+ " + KioskLang.FormatPrice(def.Price, currency),
					Font = new Font("Segoe UI Bold", 10.5f),
					FlatStyle = FlatStyle.Flat,
					Size = new Size(num - 28, 40),
					Location = new Point(14, 236),
					BackColor = KioskSkin.PillBg,
					ForeColor = KioskSkin.Accent,
					Cursor = Cursors.Hand
				};
				addPill.FlatAppearance.BorderSize = 1;
				addPill.FlatAppearance.BorderColor = KioskSkin.Accent;
				card.Click += (object s, EventArgs e) =>
				{
					ToggleSelect();
				};
				addPill.Click += (object s, EventArgs e) =>
				{
					ToggleSelect();
				};
				card.Controls.Add(addPill);
				Controls.Add(card);
				void ToggleSelect()
				{
					isSelected = !isSelected;
					if (isSelected)
					{
						if (!upsellDialog.SelectedUpsells.Contains(prod))
						{
							upsellDialog.SelectedUpsells.Add(prod);
						}
						addPill.Text = "V " + KioskLang.T("UpsellAdded");
						addPill.BackColor = KioskSkin.Accent;
						addPill.ForeColor = KioskSkin.OnAccent;
					}
					else
					{
						upsellDialog.SelectedUpsells.Remove(prod);
						addPill.Text = "+ " + KioskLang.FormatPrice(def.Price, currency);
						addPill.BackColor = KioskSkin.PillBg;
						addPill.ForeColor = KioskSkin.Accent;
					}
					card.Invalidate();
					UpdateUpsellTotal();
					void UpdateUpsellTotal()
					{
						decimal num8 = upsellDialog.SelectedUpsells.Sum((KioskProduct u) => u.Price);
						decimal price = upsellDialog.baseTotal + num8;
						if (upsellDialog.proceedBtn != null)
						{
							upsellDialog.proceedBtn.Text = KioskLang.T("ProceedWithExtras", KioskLang.FormatPrice(price, currency));
						}
					}
				}
			}
			Panel panel = new Panel
			{
				Dock = DockStyle.Bottom,
				Height = 84,
				BackColor = KioskSkin.HeaderBg
			};
			Button button = new Button
			{
				Text = KioskLang.T("SkipUpsell"),
				Font = new Font("Segoe UI Semibold", 12f),
				FlatStyle = FlatStyle.Flat,
				Size = new Size(320, 52),
				Location = new Point(36, 16),
				BackColor = KioskSkin.PillBg,
				ForeColor = KioskSkin.TextMuted,
				Cursor = Cursors.Hand
			};
			button.FlatAppearance.BorderSize = 1;
			button.FlatAppearance.BorderColor = KioskSkin.Border;
			button.Click += (object s, EventArgs e) =>
			{
				upsellDialog.DialogResult = DialogResult.OK;
				upsellDialog.Close();
			};
			proceedBtn = new Button
			{
				Text = KioskLang.T("ProceedWithExtras", KioskLang.FormatPrice(baseTotal, currency)),
				Font = new Font("Segoe UI Bold", 13f),
				FlatStyle = FlatStyle.Flat,
				Size = new Size(380, 52),
				Location = new Point(Width - 416, 16),
				BackColor = KioskSkin.Accent,
				ForeColor = KioskSkin.OnAccent,
				Cursor = Cursors.Hand
			};
			proceedBtn.FlatAppearance.BorderSize = 0;
			proceedBtn.Click += (object s, EventArgs e) =>
			{
				upsellDialog.DialogResult = DialogResult.OK;
				upsellDialog.Close();
			};
			panel.Controls.AddRange(new Control[2] { button, proceedBtn });
			Controls.Add(panel);
		}
	}
	internal static class KioskTouchKeyboard
	{
		public static bool IsEnabled { get; set; } = true;

		public static void Register()
		{
			Application.AddMessageFilter(new KioskKeyboardMessageFilter());
		}
	}
	internal sealed class KioskKeyboardMessageFilter : IMessageFilter
	{
		private const int WM_LBUTTONDOWN = 513;

		private const int WM_LBUTTONDBLCLK = 515;

		public bool PreFilterMessage(ref Message m)
		{
			if (m.Msg != 513 && m.Msg != 515)
			{
				return false;
			}
			if (KioskKeyboardForm.IsOpen || !KioskTouchKeyboard.IsEnabled || KioskMainForm.IsHeadlessSnapshot)
			{
				return false;
			}
			if (!(Control.FromHandle(m.HWnd) is TextBox textBox))
			{
				return false;
			}
			if (textBox.IsDisposed || !textBox.Enabled || !textBox.Visible || textBox.ReadOnly)
			{
				return false;
			}
			if (textBox.FindForm() is KioskKeyboardForm)
			{
				return false;
			}
			if (textBox.Tag is string text && text == "NoTouchKeyboard")
			{
				return false;
			}
			if (!textBox.IsHandleCreated)
			{
				return false;
			}
			int num = (int)m.LParam;
			Point pt = new Point((short)(num & 0xFFFF), (short)((num >> 16) & 0xFFFF));
			if (!textBox.ClientRectangle.Contains(pt))
			{
				return false;
			}
			KioskKeyboardForm.ShowFor(textBox);
			return false;
		}
	}
	internal sealed class KioskKeyboardForm : Form
	{
		private readonly TextBox target;
		private readonly bool numericOnly;
		private readonly Label previewLabel;
		private readonly Panel abcPanel = new Panel();
		private readonly Panel numPanel = new Panel();

		private readonly List<(Button Button, char Lower, char Upper)> keyBindings = new List<(Button, char, char)>();
		private Button? shiftBtnLeft;
		private Button? shiftBtnRight;
		private Button? capsBtn;
		private bool isShiftActive;
		private bool isCapsActive;
		private int caretPos;

		public static bool IsOpen { get; private set; }

		public static void ShowFor(TextBox textBox)
		{
			if (IsOpen || textBox.IsDisposed) return;
			try
			{
				IsOpen = true;
				using KioskKeyboardForm form = new KioskKeyboardForm(textBox);
				Form parent = textBox.FindForm() ?? Form.ActiveForm;
				if (parent is KioskMainForm kmf)
				{
					kmf.SetModalState(true);
					try { form.ShowDialog(parent); }
					finally { kmf.SetModalState(false); }
				}
				else
				{
					form.ShowDialog(parent);
				}
			}
			catch { }
			finally
			{
				IsOpen = false;
			}
		}

		private KioskKeyboardForm(TextBox textBox)
		{
			target = textBox;
			caretPos = target.SelectionStart;
			if (caretPos < 0 || caretPos > target.Text.Length) caretPos = target.Text.Length;

			numericOnly = textBox.PasswordChar != '\0' || 
			              (textBox.Text.Length > 0 && textBox.Text.All(char.IsDigit) && textBox.Name.IndexOf("pin", StringComparison.OrdinalIgnoreCase) >= 0);

			Text = "Clavier Virtuel Complet";
			FormBorderStyle = FormBorderStyle.None;
			ShowInTaskbar = false;
			KeyPreview = true;
			TopMost = true;
			BackColor = KioskSkin.IsDark ? Color.FromArgb(20, 26, 38) : Color.FromArgb(248, 250, 252);
			ForeColor = KioskSkin.Text;
			Size = new Size(960, 396);
			StartPosition = FormStartPosition.Manual;

			Panel topBar = new Panel
			{
				Dock = DockStyle.Top,
				Height = 56,
				BackColor = KioskSkin.IsDark ? Color.FromArgb(15, 20, 30) : Color.FromArgb(241, 245, 249)
			};

			string fieldName = string.IsNullOrWhiteSpace(target.Name) ? "Saisie" : target.Name;
			if (fieldName.StartsWith("txt", StringComparison.OrdinalIgnoreCase)) fieldName = fieldName.Substring(3);

			Label lblField = new Label
			{
				Text = fieldName,
				Location = new Point(14, 16),
				AutoSize = true,
				Font = new Font("Segoe UI Bold", 11.5f),
				ForeColor = KioskSkin.Accent
			};

			previewLabel = new Label
			{
				Location = new Point(115, 9),
				Size = new Size(690, 38),
				BorderStyle = BorderStyle.FixedSingle,
				BackColor = KioskSkin.CardBg,
				ForeColor = KioskSkin.Text,
				Font = new Font("Segoe UI", 13f, FontStyle.Bold),
				TextAlign = ContentAlignment.MiddleLeft,
				AutoEllipsis = true,
				Padding = new Padding(8, 0, 8, 0)
			};

			Button btnDone = new Button
			{
				Text = "✔ " + KioskLang.T("Done"),
				Location = new Point(818, 9),
				Size = new Size(128, 38),
				BackColor = KioskSkin.Accent,
				ForeColor = KioskSkin.OnAccent,
				FlatStyle = FlatStyle.Flat,
				Font = new Font("Segoe UI Bold", 11f),
				Cursor = Cursors.Hand
			};
			btnDone.FlatAppearance.BorderSize = 0;
			btnDone.Click += (s, e) => { DialogResult = DialogResult.OK; Close(); };
			AcceptButton = btnDone;

			topBar.Controls.AddRange(new Control[] { lblField, previewLabel, btnDone });

			abcPanel.SetBounds(0, 56, 960, 340);
			abcPanel.BackColor = BackColor;

			numPanel.SetBounds(0, 56, 960, 340);
			numPanel.BackColor = BackColor;

			BuildPhysicalAbcPanel();
			BuildPhysicalNumericPanel();

			Controls.AddRange(new Control[] { topBar, abcPanel, numPanel });
			abcPanel.Visible = !numericOnly;
			numPanel.Visible = numericOnly;

			Paint += (s, pe) =>
			{
				using Pen pen = new Pen(KioskSkin.Accent, 2f);
				pe.Graphics.DrawRectangle(pen, 0, 0, Width - 1, Height - 1);
			};
		}

		protected override void OnShown(EventArgs e)
		{
			base.OnShown(e);
			caretPos = target.Text.Length;
			UpdatePreview();
			Rectangle bounds = (Screen.FromControl(target) ?? Screen.PrimaryScreen).Bounds;
			int x = bounds.Left + Math.Max(10, (bounds.Width - Width) / 2);
			int y = bounds.Top + bounds.Height - Height - 16;
			Location = new Point(x, y);
		}

		protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
		{
			Keys keys = keyData & Keys.KeyCode;
			switch (keys)
			{
				case Keys.Escape:
					Close();
					return true;
				case Keys.Return:
					DialogResult = DialogResult.OK;
					Close();
					return true;
				case Keys.Back:
					Backspace();
					return true;
				case Keys.Delete:
					DeleteChar();
					return true;
				case Keys.Left:
					MoveLeft();
					return true;
				case Keys.Right:
					MoveRight();
					return true;
				case Keys.Home:
					MoveHome();
					return true;
				case Keys.End:
					MoveEnd();
					return true;
				default:
					return base.ProcessCmdKey(ref msg, keyData);
			}
		}

		private Button MakeKey(Panel parent, string label, int x, int y, int w, int h, Color bg, float fontSize = 11f, Action? onClick = null)
		{
			Button btn = new Button
			{
				Text = label,
				Location = new Point(x, y),
				Size = new Size(w, h),
				BackColor = bg,
				ForeColor = KioskSkin.Text,
				FlatStyle = FlatStyle.Flat,
				Font = new Font("Segoe UI Semibold", fontSize),
				Cursor = Cursors.Hand
			};
			btn.FlatAppearance.BorderSize = 0;
			if (onClick != null) btn.Click += (s, e) => onClick();
			parent.Controls.Add(btn);
			return btn;
		}

		private void RegisterCharKey(Button btn, char lower, char upper)
		{
			keyBindings.Add((btn, lower, upper));
			btn.Click += (s, e) =>
			{
				bool uppercase = isCapsActive ^ isShiftActive;
				char ch = uppercase ? upper : lower;
				InsertChar(ch);
			};
		}

		private void BuildPhysicalAbcPanel()
		{
			Color keyBg = KioskSkin.IsDark ? Color.FromArgb(32, 40, 56) : Color.White;
			Color modBg = KioskSkin.IsDark ? Color.FromArgb(45, 55, 75) : Color.FromArgb(226, 232, 240);
			Color navBg = KioskSkin.IsDark ? Color.FromArgb(48, 62, 90) : Color.FromArgb(215, 225, 240);

			int startX = 10;
			int startY = 8;
			int keyH = 56;
			int gap = 6;

			// ---------------- Row 0: Esc + Numbers + Backspace + Del + Home ----------------
			int curX = startX;
			int curY = startY;

			MakeKey(abcPanel, "Esc", curX, curY, 52, keyH, modBg, 10, () => Close());
			curX += 52 + gap;

			(char Lower, char Upper)[] numRow = new (char, char)[]
			{
				('1', '!'), ('2', '@'), ('3', '#'), ('4', '$'), ('5', '%'),
				('6', '^'), ('7', '&'), ('8', '*'), ('9', '('), ('0', ')'),
				('-', '_'), ('=', '+')
			};

			foreach (var pair in numRow)
			{
				Button b = MakeKey(abcPanel, pair.Lower.ToString(), curX, curY, 52, keyH, keyBg, 12);
				RegisterCharKey(b, pair.Lower, pair.Upper);
				curX += 52 + gap;
			}

			MakeKey(abcPanel, "⌫", curX, curY, 82, keyH, modBg, 15, () => Backspace());
			curX += 82 + gap;

			MakeKey(abcPanel, "Del", curX, curY, 52, keyH, navBg, 10, () => DeleteChar());
			curX += 52 + gap;

			MakeKey(abcPanel, "Home", curX, curY, 52, keyH, navBg, 9.5f, () => MoveHome());

			// ---------------- Row 1: Tab + QWERTY + End ----------------
			curX = startX;
			curY += keyH + gap;

			MakeKey(abcPanel, "Tab ⇥", curX, curY, 70, keyH, modBg, 9.5f, () => InsertChar('	'));
			curX += 70 + gap;

			(char Lower, char Upper)[] row1 = new (char Lower, char Upper)[]
			{
				('q', 'Q'), ('w', 'W'), ('e', 'E'), ('r', 'R'), ('t', 'T'),
				('y', 'Y'), ('u', 'U'), ('i', 'I'), ('o', 'O'), ('p', 'P'),
				('[', '{'), (']', '}'), ('\\', '|')
			};

			foreach (var pair in row1)
			{
				Button b = MakeKey(abcPanel, pair.Lower.ToString(), curX, curY, 52, keyH, keyBg, 12);
				RegisterCharKey(b, pair.Lower, pair.Upper);
				curX += 52 + gap;
			}

			MakeKey(abcPanel, "End", curX, curY, 52, keyH, navBg, 9.5f, () => MoveEnd());

			// ---------------- Row 2: Caps + ASDFGHJKL + Enter + Clear ----------------
			curX = startX;
			curY += keyH + gap;

			capsBtn = MakeKey(abcPanel, "Caps Lock", curX, curY, 82, keyH, modBg, 9.5f, () => ToggleCaps());
			curX += 82 + gap;

			(char Lower, char Upper)[] row2 = new (char Lower, char Upper)[]
			{
				('a', 'A'), ('s', 'S'), ('d', 'D'), ('f', 'F'), ('g', 'G'),
				('h', 'H'), ('j', 'J'), ('k', 'K'), ('l', 'L'), (';', ':'), ('\'', '"')
			};

			foreach (var pair in row2)
			{
				Button b = MakeKey(abcPanel, pair.Lower.ToString(), curX, curY, 52, keyH, keyBg, 12);
				RegisterCharKey(b, pair.Lower, pair.Upper);
				curX += 52 + gap;
			}

			Button enterBtn = MakeKey(abcPanel, "Enter ↵", curX, curY, 104, keyH, KioskSkin.Accent, 11, () =>
			{
				DialogResult = DialogResult.OK;
				Close();
			});
			enterBtn.ForeColor = KioskSkin.OnAccent;
			curX += 104 + gap;

			MakeKey(abcPanel, "Clear", curX, curY, 52, keyH, modBg, 9.5f, () => ClearAll());

			// ---------------- Row 3: Shift + ZXCVBNM + Shift + Up Arrow ----------------
			curX = startX;
			curY += keyH + gap;

			shiftBtnLeft = MakeKey(abcPanel, "Shift ⇧", curX, curY, 96, keyH, modBg, 10, () => ToggleShift());
			curX += 96 + gap;

			(char Lower, char Upper)[] row3 = new (char Lower, char Upper)[]
			{
				('z', 'Z'), ('x', 'X'), ('c', 'C'), ('v', 'V'), ('b', 'B'),
				('n', 'N'), ('m', 'M'), (',', '<'), ('.', '>'), ('/', '?')
			};

			foreach (var pair in row3)
			{
				Button b = MakeKey(abcPanel, pair.Lower.ToString(), curX, curY, 52, keyH, keyBg, 12);
				RegisterCharKey(b, pair.Lower, pair.Upper);
				curX += 52 + gap;
			}

			shiftBtnRight = MakeKey(abcPanel, "Shift ⇧", curX, curY, 90, keyH, modBg, 10, () => ToggleShift());
			curX += 90 + gap;

			MakeKey(abcPanel, "↑", curX, curY, 52, keyH, navBg, 16, () => MoveHome());

			// ---------------- Row 4: Ctrl + Alt + Space + AltGr + Close + Left + Down + Right ----------------
			curX = startX;
			curY += keyH + gap;

			MakeKey(abcPanel, "Ctrl", curX, curY, 66, keyH, modBg, 10);
			curX += 66 + gap;

			MakeKey(abcPanel, "Alt", curX, curY, 66, keyH, modBg, 10);
			curX += 66 + gap;

			MakeKey(abcPanel, "Espace", curX, curY, 360, keyH, keyBg, 12, () => InsertChar(' '));
			curX += 360 + gap;

			MakeKey(abcPanel, "AltGr", curX, curY, 66, keyH, modBg, 10);
			curX += 66 + gap;

			MakeKey(abcPanel, "Masquer ✕", curX, curY, 80, keyH, modBg, 9.5f, () => Close());
			curX += 80 + gap;

			MakeKey(abcPanel, "←", curX, curY, 52, keyH, navBg, 16, () => MoveLeft());
			curX += 52 + gap;

			MakeKey(abcPanel, "↓", curX, curY, 52, keyH, navBg, 16, () => MoveEnd());
			curX += 52 + gap;

			MakeKey(abcPanel, "→", curX, curY, 52, keyH, navBg, 16, () => MoveRight());
		}

		private void BuildPhysicalNumericPanel()
		{
			Color keyBg = KioskSkin.IsDark ? Color.FromArgb(32, 40, 56) : Color.White;
			Color modBg = KioskSkin.IsDark ? Color.FromArgb(45, 55, 75) : Color.FromArgb(226, 232, 240);
			Color navBg = KioskSkin.IsDark ? Color.FromArgb(48, 62, 90) : Color.FromArgb(215, 225, 240);

			int startX = (numPanel.Width - 540) / 2;
			int startY = 12;

			string[] numKeys = new string[]
			{
				"7", "8", "9",
				"4", "5", "6",
				"1", "2", "3",
				"0", ".", "⌫"
			};

			for (int i = 0; i < numKeys.Length; i++)
			{
				string k = numKeys[i];
				int col = i % 3;
				int row = i / 3;
				Button btn = new Button
				{
					Text = k,
					Location = new Point(startX + col * 180, startY + row * 60),
					Size = new Size(172, 54),
					BackColor = (k == "⌫") ? modBg : keyBg,
					ForeColor = KioskSkin.Text,
					FlatStyle = FlatStyle.Flat,
					Font = new Font("Segoe UI Bold", (k == "⌫") ? 16f : 20f),
					Cursor = Cursors.Hand
				};
				btn.FlatAppearance.BorderSize = 0;
				if (k == "⌫")
				{
					btn.Click += (s, e) => Backspace();
				}
				else
				{
					char ch = k[0];
					btn.Click += (s, e) => InsertChar(ch);
				}
				numPanel.Controls.Add(btn);
			}

			Button btnLeft = new Button
			{
				Text = "←",
				Location = new Point(startX, startY + 246),
				Size = new Size(82, 54),
				BackColor = navBg,
				ForeColor = KioskSkin.Text,
				FlatStyle = FlatStyle.Flat,
				Font = new Font("Segoe UI Bold", 16f),
				Cursor = Cursors.Hand
			};
			btnLeft.FlatAppearance.BorderSize = 0;
			btnLeft.Click += (s, e) => MoveLeft();

			Button btnRight = new Button
			{
				Text = "→",
				Location = new Point(startX + 88, startY + 246),
				Size = new Size(82, 54),
				BackColor = navBg,
				ForeColor = KioskSkin.Text,
				FlatStyle = FlatStyle.Flat,
				Font = new Font("Segoe UI Bold", 16f),
				Cursor = Cursors.Hand
			};
			btnRight.FlatAppearance.BorderSize = 0;
			btnRight.Click += (s, e) => MoveRight();

			Button btnClear = new Button
			{
				Text = "Effacer",
				Location = new Point(startX + 180, startY + 246),
				Size = new Size(172, 54),
				BackColor = modBg,
				ForeColor = KioskSkin.Text,
				FlatStyle = FlatStyle.Flat,
				Font = new Font("Segoe UI Semibold", 12f),
				Cursor = Cursors.Hand
			};
			btnClear.FlatAppearance.BorderSize = 0;
			btnClear.Click += (s, e) => ClearAll();

			Button btnVal = new Button
			{
				Text = "✔ " + KioskLang.T("Done"),
				Location = new Point(startX + 360, startY + 246),
				Size = new Size(172, 54),
				BackColor = KioskSkin.Accent,
				ForeColor = KioskSkin.OnAccent,
				FlatStyle = FlatStyle.Flat,
				Font = new Font("Segoe UI Bold", 13.5f),
				Cursor = Cursors.Hand
			};
			btnVal.FlatAppearance.BorderSize = 0;
			btnVal.Click += (s, e) => { DialogResult = DialogResult.OK; Close(); };

			numPanel.Controls.AddRange(new Control[] { btnLeft, btnRight, btnClear, btnVal });
		}

		private void ToggleShift()
		{
			isShiftActive = !isShiftActive;
			RefreshKeyLabels();
		}

		private void ToggleCaps()
		{
			isCapsActive = !isCapsActive;
			RefreshKeyLabels();
		}

		private void RefreshKeyLabels()
		{
			Color modBg = KioskSkin.IsDark ? Color.FromArgb(45, 55, 75) : Color.FromArgb(226, 232, 240);
			Color activeBg = KioskSkin.Accent;

			if (shiftBtnLeft != null)
			{
				shiftBtnLeft.BackColor = isShiftActive ? activeBg : modBg;
				shiftBtnLeft.ForeColor = isShiftActive ? KioskSkin.OnAccent : KioskSkin.Text;
			}
			if (shiftBtnRight != null)
			{
				shiftBtnRight.BackColor = isShiftActive ? activeBg : modBg;
				shiftBtnRight.ForeColor = isShiftActive ? KioskSkin.OnAccent : KioskSkin.Text;
			}
			if (capsBtn != null)
			{
				capsBtn.BackColor = isCapsActive ? activeBg : modBg;
				capsBtn.ForeColor = isCapsActive ? KioskSkin.OnAccent : KioskSkin.Text;
			}

			bool uppercase = isCapsActive ^ isShiftActive;
			foreach (var (btn, lower, upper) in keyBindings)
			{
				btn.Text = (uppercase ? upper : lower).ToString();
			}
		}

		private void InsertChar(char c)
		{
			int pos = Math.Max(0, Math.Min(caretPos, target.Text.Length));
			target.Text = target.Text.Insert(pos, c.ToString());
			caretPos = pos + 1;
			target.SelectionStart = caretPos;
			if (isShiftActive && !isCapsActive)
			{
				isShiftActive = false;
				RefreshKeyLabels();
			}
			UpdatePreview();
		}

		private void Backspace()
		{
			if (caretPos > 0 && target.Text.Length > 0)
			{
				int pos = Math.Min(caretPos, target.Text.Length);
				target.Text = target.Text.Remove(pos - 1, 1);
				caretPos = pos - 1;
				target.SelectionStart = caretPos;
				UpdatePreview();
			}
		}

		private void DeleteChar()
		{
			if (caretPos < target.Text.Length)
			{
				target.Text = target.Text.Remove(caretPos, 1);
				target.SelectionStart = caretPos;
				UpdatePreview();
			}
		}

		private void MoveLeft()
		{
			if (caretPos > 0)
			{
				caretPos--;
				target.SelectionStart = caretPos;
				UpdatePreview();
			}
		}

		private void MoveRight()
		{
			if (caretPos < target.Text.Length)
			{
				caretPos++;
				target.SelectionStart = caretPos;
				UpdatePreview();
			}
		}

		private void MoveHome()
		{
			caretPos = 0;
			target.SelectionStart = 0;
			UpdatePreview();
		}

		private void MoveEnd()
		{
			caretPos = target.Text.Length;
			target.SelectionStart = caretPos;
			UpdatePreview();
		}

		private void ClearAll()
		{
			target.Clear();
			caretPos = 0;
			UpdatePreview();
		}

		private void UpdatePreview()
		{
			string raw = target.Text;
			int p = Math.Max(0, Math.Min(caretPos, raw.Length));
			if (target.PasswordChar != '\0')
			{
				raw = new string('●', raw.Length);
			}
			previewLabel.Text = raw.Insert(p, "|");
		}
	}

	internal sealed class KioskTouchScrollFilter : IMessageFilter
	{
		[DllImport("user32.dll")]
		private static extern bool ReleaseCapture();

		private const int WM_LBUTTONDOWN = 0x0201;
		private const int WM_MOUSEMOVE   = 0x0200;
		private const int WM_LBUTTONUP   = 0x0202;
		private const int WM_MOUSEWHEEL  = 0x020A;

		private bool isDown;
		private bool isDragging;
		private Point startPoint;
		private Point startScroll;
		private ScrollableControl? activeContainer;

		public static event Action<ScrollableControl>? ContainerScrolled;

		public static void NotifyScrolled(ScrollableControl? c)
		{
			if (c != null)
			{
				ContainerScrolled?.Invoke(c);
			}
		}

		public bool PreFilterMessage(ref Message m)
		{
			if (KioskMainForm.IsHeadlessSnapshot) return false;

			if (m.Msg == WM_MOUSEWHEEL)
			{
				Control c = Control.FromHandle(m.HWnd);
				ScrollableControl? sc = FindScrollableParent(c);
				if (sc != null)
				{
					NotifyScrolled(sc);
				}
				return false;
			}

			if (m.Msg == WM_LBUTTONDOWN)
			{
				Control c = Control.FromHandle(m.HWnd);
				if (c is KioskSleekScrollBar || c is TextBox)
				{
					activeContainer = null;
					isDown = false;
					return false;
				}

				activeContainer = FindScrollableParent(c);
				if (activeContainer != null)
				{
					isDown = true;
					isDragging = false;
					startPoint = Cursor.Position;
					startScroll = new Point(-activeContainer.AutoScrollPosition.X, -activeContainer.AutoScrollPosition.Y);
				}
				return false;
			}
			else if (m.Msg == WM_MOUSEMOVE)
			{
				if (isDown && activeContainer != null && !activeContainer.IsDisposed)
				{
					int dx = Cursor.Position.X - startPoint.X;
					int dy = Cursor.Position.Y - startPoint.Y;

					if (!isDragging)
					{
						if (Math.Abs(dx) > 10 || Math.Abs(dy) > 10)
						{
							isDragging = true;
							ReleaseCapture();
						}
					}

					if (isDragging)
					{
						int newX = Math.Max(0, startScroll.X - dx);
						int newY = Math.Max(0, startScroll.Y - dy);
						activeContainer.AutoScrollPosition = new Point(newX, newY);
						NotifyScrolled(activeContainer);
						return true;
					}
				}
			}
			else if (m.Msg == WM_LBUTTONUP)
			{
				if (isDown)
				{
					isDown = false;
					if (isDragging)
					{
						isDragging = false;
						ReleaseCapture();
						return true;
					}
				}
			}

			return false;
		}

		private static ScrollableControl? FindScrollableParent(Control? c)
		{
			while (c != null)
			{
				if (c is TextBox) return null;
				if (c is KioskSleekScrollBar) return null;
				if (c is FlowLayoutPanel flp && flp.AutoScroll) return flp;
				if (c is Panel p && p.AutoScroll) return p;
				c = c.Parent;
			}
			return null;
		}
	}

	internal class KioskCleanFlowPanel : FlowLayoutPanel
	{
		[DllImport("user32.dll")]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool ShowScrollBar(IntPtr hWnd, int wBar, [MarshalAs(UnmanagedType.Bool)] bool bShow);

		public const int SB_HORZ = 0;
		public const int SB_VERT = 1;
		public const int SB_BOTH = 3;

		public bool HideHorizontalBar { get; set; } = true;
		public bool HideVerticalBar { get; set; } = true;

		public KioskCleanFlowPanel()
		{
			DoubleBuffered = true;
		}

		protected override void WndProc(ref Message m)
		{
			base.WndProc(ref m);
			if (m.Msg == 0x0085 /* WM_NCPAINT */)
			{
				if (HideHorizontalBar && HideVerticalBar)
				{
					ShowScrollBar(Handle, SB_BOTH, false);
				}
				else if (HideHorizontalBar)
				{
					ShowScrollBar(Handle, SB_HORZ, false);
				}
				else if (HideVerticalBar)
				{
					ShowScrollBar(Handle, SB_VERT, false);
				}
			}
		}
	}

	internal sealed class KioskSleekScrollBar : Control
	{
		private readonly ScrollableControl target;
		private bool isDraggingThumb;
		private int dragStartY;
		private int dragStartScrollY;

		public KioskSleekScrollBar(ScrollableControl target)
		{
			this.target = target;
			SetStyle(ControlStyles.AllPaintingInWmPaint |
					 ControlStyles.UserPaint |
					 ControlStyles.OptimizedDoubleBuffer |
					 ControlStyles.SupportsTransparentBackColor, true);
			BackColor = Color.Transparent;
			Width = 6;
			DoubleBuffered = true;
			Cursor = Cursors.Default;

			if (target != null)
			{
				target.SizeChanged += (s, e) => UpdatePosition();
				target.LocationChanged += (s, e) => UpdatePosition();
				target.Scroll += (s, e) => Invalidate();
			}

			KioskTouchScrollFilter.ContainerScrolled += OnContainerScrolled;
		}

		private void OnContainerScrolled(ScrollableControl sc)
		{
			if (sc == target && IsHandleCreated && !IsDisposed)
			{
				Invalidate();
			}
		}

		public void UpdatePosition()
		{
			if (target == null || target.Parent == null || target.IsDisposed) return;

			int totalH = target.DisplayRectangle.Height;
			int visH = target.ClientSize.Height;
			if (totalH <= visH || visH <= 20)
			{
				Visible = false;
				return;
			}

			Visible = true;
			int x = target.Right - Width - 3;
			int y = target.Top + 4;
			int h = Math.Max(20, visH - 8);

			SetBounds(x, y, Width, h);
			BringToFront();
			Invalidate();
		}

		protected override void OnMouseDown(MouseEventArgs e)
		{
			base.OnMouseDown(e);
			if (e.Button == MouseButtons.Left && target != null)
			{
				isDraggingThumb = true;
				dragStartY = e.Y;
				dragStartScrollY = -target.AutoScrollPosition.Y;
				Capture = true;
			}
		}

		protected override void OnMouseMove(MouseEventArgs e)
		{
			base.OnMouseMove(e);
			if (isDraggingThumb && target != null)
			{
				int totalH = target.DisplayRectangle.Height;
				int visH = target.ClientSize.Height;
				int maxOffset = totalH - visH;
				int thumbH = Math.Max(28, (int)((float)visH / totalH * Height));
				int availableTrack = Height - thumbH;
				if (availableTrack > 0 && maxOffset > 0)
				{
					int deltaY = e.Y - dragStartY;
					int newScrollY = dragStartScrollY + (int)((float)deltaY / availableTrack * maxOffset);
					newScrollY = Math.Max(0, Math.Min(maxOffset, newScrollY));
					target.AutoScrollPosition = new Point(-target.AutoScrollPosition.X, newScrollY);
					Invalidate();
				}
			}
		}

		protected override void OnMouseUp(MouseEventArgs e)
		{
			base.OnMouseUp(e);
			if (isDraggingThumb)
			{
				isDraggingThumb = false;
				Capture = false;
			}
		}

		protected override void OnPaint(PaintEventArgs e)
		{
			if (target == null) return;
			int totalH = target.DisplayRectangle.Height;
			int visH = target.ClientSize.Height;
			if (totalH <= visH || Height <= 0) return;

			int thumbH = Math.Max(28, (int)((float)visH / totalH * Height));
			int maxOffset = totalH - visH;
			int scrollY = -target.AutoScrollPosition.Y;
			int thumbY = maxOffset > 0 ? (int)((float)scrollY / maxOffset * (Height - thumbH)) : 0;
			thumbY = Math.Max(0, Math.Min(Height - thumbH, thumbY));

			e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
			Color pillColor = KioskSkin.IsDark
				? Color.FromArgb(85, 160, 180, 210)
				: Color.FromArgb(75, 100, 116, 139);

			using SolidBrush brush = new SolidBrush(pillColor);
			using GraphicsPath path = new GraphicsPath();
			Rectangle r = new Rectangle(0, thumbY, Width - 1, thumbH);
			int radius = (Width - 1) / 2;
			path.AddArc(r.X, r.Y, radius * 2, radius * 2, 180, 90);
			path.AddArc(r.Right - radius * 2, r.Y, radius * 2, radius * 2, 270, 90);
			path.AddArc(r.Right - radius * 2, r.Bottom - radius * 2, radius * 2, radius * 2, 0, 90);
			path.AddArc(r.X, r.Bottom - radius * 2, radius * 2, radius * 2, 90, 90);
			path.CloseFigure();
			e.Graphics.FillPath(brush, path);
		}
	}

	internal static class KioskTaskbar
	{
		private const int SW_HIDE = 0;

		private const int SW_SHOW = 5;

		[DllImport("user32.dll", SetLastError = true)]
		private static extern IntPtr FindWindow(string lpClassName, string? lpWindowName);

		[DllImport("user32.dll", SetLastError = true)]
		private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

		public static void Hide()
		{
			try
			{
				IntPtr intPtr = FindWindow("Shell_TrayWnd", null);
				if (intPtr != IntPtr.Zero)
				{
					ShowWindow(intPtr, 0);
				}
				IntPtr intPtr2 = FindWindow("Button", "Start");
				if (intPtr2 != IntPtr.Zero)
				{
					ShowWindow(intPtr2, 0);
				}
				IntPtr intPtr3 = FindWindow("Shell_SecondaryTrayWnd", null);
				if (intPtr3 != IntPtr.Zero)
				{
					ShowWindow(intPtr3, 0);
				}
			}
			catch
			{
			}
		}

		public static void Restore()
		{
			try
			{
				IntPtr intPtr = FindWindow("Shell_TrayWnd", null);
				if (intPtr != IntPtr.Zero)
				{
					ShowWindow(intPtr, 5);
				}
				IntPtr intPtr2 = FindWindow("Button", "Start");
				if (intPtr2 != IntPtr.Zero)
				{
					ShowWindow(intPtr2, 5);
				}
				IntPtr intPtr3 = FindWindow("Shell_SecondaryTrayWnd", null);
				if (intPtr3 != IntPtr.Zero)
				{
					ShowWindow(intPtr3, 5);
				}
			}
			catch
			{
			}
		}
	}
	internal static class KioskKeyboardLockdown
	{
		private struct KBDLLHOOKSTRUCT
		{
			public int vkCode;

			public int scanCode;

			public int flags;

			public int time;

			public IntPtr dwExtraInfo;
		}

		private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

		private const int WH_KEYBOARD_LL = 13;

		private const int VK_TAB = 9;

		private const int VK_ESCAPE = 27;

		private const int VK_F4 = 115;

		private const int VK_LWIN = 91;

		private const int VK_RWIN = 92;

		private const int VK_APPS = 93;

		private const int VK_CONTROL = 17;

		private const int VK_SHIFT = 16;

		private static IntPtr hookId = IntPtr.Zero;

		private static LowLevelKeyboardProc? hookProc;

		public static KioskMainForm ActiveForm { get; set; }

		[DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
		private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

		[DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		private static extern bool UnhookWindowsHookEx(IntPtr hhk);

		[DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
		private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

		[DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
		private static extern IntPtr GetModuleHandle(string? lpModuleName);

		[DllImport("user32.dll")]
		private static extern short GetAsyncKeyState(int vKey);

		public static void Install()
		{
			if (hookId != IntPtr.Zero)
			{
				return;
			}
			try
			{
				hookProc = HookCallback;
				IntPtr moduleHandle = GetModuleHandle(null);
				hookId = SetWindowsHookEx(13, hookProc, moduleHandle, 0u);
			}
			catch
			{
			}
		}

		public static void Uninstall()
		{
			if (hookId != IntPtr.Zero)
			{
				try
				{
					UnhookWindowsHookEx(hookId);
				}
				catch
				{
				}
				hookId = IntPtr.Zero;
				hookProc = null;
			}
		}

		private static IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
		{
			if (nCode >= 0)
			{
				try
				{
					KBDLLHOOKSTRUCT kBDLLHOOKSTRUCT = (KBDLLHOOKSTRUCT)Marshal.PtrToStructure(lParam, typeof(KBDLLHOOKSTRUCT));
					bool flag = (kBDLLHOOKSTRUCT.flags & 0x20) != 0;
					bool flag2 = (GetAsyncKeyState(17) & 0x8000) != 0;
					bool flag3 = (GetAsyncKeyState(16) & 0x8000) != 0;
					if (flag && kBDLLHOOKSTRUCT.vkCode == 115)
					{
						if (ActiveForm != null && !ActiveForm.IsDisposed)
						{
							ActiveForm.BeginInvoke((Action)(() =>
							{
								ActiveForm.PromptExitViaPin();
							}));
						}
						return (IntPtr)1;
					}
					if (kBDLLHOOKSTRUCT.vkCode == 91 || kBDLLHOOKSTRUCT.vkCode == 92 || kBDLLHOOKSTRUCT.vkCode == 93)
					{
						return (IntPtr)1;
					}
					if (flag && (kBDLLHOOKSTRUCT.vkCode == 9 || kBDLLHOOKSTRUCT.vkCode == 27))
					{
						return (IntPtr)1;
					}
					if (flag2 && kBDLLHOOKSTRUCT.vkCode == 27)
					{
						return (IntPtr)1;
					}
					if ((flag2 & flag3) && kBDLLHOOKSTRUCT.vkCode == 27)
					{
						return (IntPtr)1;
					}
				}
				catch
				{
				}
			}
			return CallNextHookEx(hookId, nCode, wParam, lParam);
		}
	}
	internal sealed class KioskMainForm : Form
	{
		public static bool IsHeadlessSnapshot;

		private bool isAuthorizedExit;

		private bool isModalOpen;

		private readonly KioskConfig config;

		public KioskConfig Config => config;

		public List<KioskProduct> Products => products;

		private readonly HttpClient http = new HttpClient
		{
			Timeout = TimeSpan.FromSeconds(5.0)
		};

		private Panel screenContainer;

		private Panel timeoutWarningOverlay;

		private Label timeoutCountLbl;

		private LockScreenPanel lockScreenPanel;

		private Panel connectionPanel;

		private Panel serviceChoicePanel;

		private Panel catalogPanel;

		private Panel confirmationPanel;

		private Label serviceChoiceBrandLogo;

		private Label serviceChoiceStatusBadge;

		private Button serviceChoiceCancelBtn;

		private Button scFrBtn;

		private Button scEnBtn;

		private Button scArBtn;

		private Button scThemeBtn;

		private Label serviceChoiceTitleLbl;

		private Label serviceChoiceSubLbl;

		private ServiceChoiceCard dineInChoiceCard;

		private ServiceChoiceCard takeawayChoiceCard;

		private FlowLayoutPanel categoryCardsBar;

		private TextBox txtSearch;

		private FlowLayoutPanel prodGrid;

		private Panel basketPanel;

		private FlowLayoutPanel basketList;

		private Panel? basketHeaderPanel;
		private Panel? basketFooterPanel;
		private Panel? basketDividerPanel;
		private Button? basketTableBtn;
		private Button? basketTypeBtn;
		private Button? basketClearBtn;
		private Panel? catalogTopBar;
		private Label? catalogDashLbl;
		private KioskSleekScrollBar? prodGridSleekBar;

		private (int CardWidth, int CardHeight, int Margin, int Cols) CalculateResponsiveCardSize(int availableWidth, bool isPortrait)
		{
			int netW = Math.Max(280, availableWidth - 64);
			int cols;
			if (isPortrait)
			{
				if (netW >= 960) cols = 3;
				else cols = 2;
			}
			else
			{
				if (netW >= 1500) cols = 5;
				else if (netW >= 1150) cols = 4;
				else cols = 3;
			}

			int margin = cols >= 3 ? 6 : 4;
			int totalMargins = cols * (margin * 2);
			int cardW = Math.Max(180, (netW - totalMargins) / cols);
			int cardH = (int)(cardW * 1.15f);
			return (cardW, cardH, margin, cols);
		}

		private Label basketSubtotalLbl;

		private Label basketTaxLbl;

		private Label basketTotalLbl;

		private Button checkoutBtn;

		private Label basketEmptyLbl;

		private Label catalogBrandLogo;

		private Label catalogStatusBadge;

		private Label catalogDatePill;

		private Label catalogTimePill;

		private Button catFrBtn;

		private Button catEnBtn;

		private Button catArBtn;

		private Button catThemeBtn;

		private Button catCloseBtn;

		private Label confirmOrderNumLbl;

		private Label confirmDescLbl;

		private Label confirmCountLbl;

		private Button confirmDoneBtn;

		private Panel confirmationCard;

		private readonly List<KioskProduct> products = new List<KioskProduct>();

		private readonly List<string> categories = new List<string>();

		private string activeCategory = "All";

		private string searchFilter = "";

		private readonly List<(KioskProduct P, int Qty, string Note)> cart = new List<(KioskProduct, int, string)>();

		private string selectedOrderType = "Dine-in";

		private string selectedTable = "Table 01";

		private string customerName = "Guest #042";

		private string restaurantName = "VECTRON RESTAURANT";

		private string currency = "EUR";

		private decimal tvaRate = 10.0m;

		private bool isOnline;

		private readonly Timer idleTimer = new Timer();

		private int idleSeconds;

		private readonly Timer heartbeatTimer = new Timer();

		private readonly Timer confirmationTimer = new Timer();

		private readonly Timer clockTimer = new Timer();

		private int confirmSecondsLeft = 8;

		private DateTime lastTapTime = DateTime.MinValue;

		private int tapCount;

		public string RestaurantName => restaurantName;

		public string Currency => currency;

		public void ReturnToLockScreen()
		{
			ResetIdle();
			cart.Clear();
			RenderCart();
			ShowScreen(lockScreenPanel);
		}

		public void SetModalState(bool open)
		{
			isModalOpen = open;
		}

		public void PromptExitViaPin()
		{
			OpenManagerPinDialog(isDirectExitRequest: true);
		}

		public KioskMainForm()
		{
			config = KioskConfig.Load();
			KioskLang.Current = config.Language;
			KioskSkin.Theme = ((!(config.Theme == "Dark")) ? KioskThemeMode.Light : KioskThemeMode.Dark);
			KioskSkin.Quality = ((config.GraphicsQuality == "Lite") ? KioskQualityTier.Lite : KioskQualityTier.High);
			Text = "Vectron Kiosk";
			FormBorderStyle = FormBorderStyle.None;
			if (!IsHeadlessSnapshot)
			{
				TopMost = true;
				WindowState = FormWindowState.Normal;
				Bounds = Screen.PrimaryScreen?.Bounds ?? Bounds;
			}
			else
			{
				WindowState = FormWindowState.Normal;
			}
			BackColor = KioskSkin.Bg;
			ForeColor = KioskSkin.Text;
			DoubleBuffered = true;
			KeyPreview = true;
			RightToLeft = ((KioskLang.Current == "ar") ? RightToLeft.Yes : RightToLeft.No);
			BuildUi();
			LoadLocalCache();
			if (!IsHeadlessSnapshot)
			{
				idleTimer.Interval = 1000;
				idleTimer.Tick += OnIdleTick;
				idleTimer.Start();
				heartbeatTimer.Interval = 12000;
				heartbeatTimer.Tick += async (object s, EventArgs e) =>
				{
					await CheckServerHeartbeatAsync();
				};
				heartbeatTimer.Start();
				clockTimer.Interval = 1000;
				clockTimer.Tick += (object s, EventArgs e) =>
				{
					UpdateLiveClock();
				};
				clockTimer.Start();
			}
			Resize += (object s, EventArgs e) =>
			{
				HandleFormResize();
			};
		}

		protected override void OnLoad(EventArgs e)
		{
			base.OnLoad(e);
			if (!IsHeadlessSnapshot)
			{
				TopMost = true;
				WindowState = FormWindowState.Normal;
				Bounds = Screen.PrimaryScreen?.Bounds ?? Bounds;
				KioskTaskbar.Hide();
				KioskKeyboardLockdown.ActiveForm = this;
				KioskKeyboardLockdown.Install();
			}
			RelayoutCurrentScreen();
			if (!IsHeadlessSnapshot)
			{
				StartupConnectAsync();
			}
		}

		protected override void OnDeactivate(EventArgs e)
		{
			base.OnDeactivate(e);
			if (IsHeadlessSnapshot || isAuthorizedExit || isModalOpen)
			{
				return;
			}
			try
			{
				BeginInvoke((Action)(() =>
				{
					if (!isModalOpen)
					{
						TopMost = true;
						BringToFront();
						Activate();
					}
				}));
			}
			catch
			{
			}
		}

		protected override void OnFormClosing(FormClosingEventArgs e)
		{
			if (IsHeadlessSnapshot)
			{
				base.OnFormClosing(e);
			}
			else if (isAuthorizedExit)
			{
				KioskKeyboardLockdown.Uninstall();
				KioskTaskbar.Restore();
				base.OnFormClosing(e);
			}
			else if (e.CloseReason == CloseReason.UserClosing || e.CloseReason == CloseReason.None)
			{
				e.Cancel = true;
			}
			else
			{
				KioskKeyboardLockdown.Uninstall();
				KioskTaskbar.Restore();
				base.OnFormClosing(e);
			}
		}

		protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
		{
			if (!IsHeadlessSnapshot)
			{
				switch (keyData)
				{
				case Keys.F4 | Keys.Alt:
					PromptExitViaPin();
					return true;
				case Keys.LWin:
				case Keys.RWin:
				case Keys.Escape | Keys.Control:
				case Keys.Tab | Keys.Alt:
				case Keys.Escape | Keys.Alt:
					return true;
				}
			}
			return base.ProcessCmdKey(ref msg, keyData);
		}

		private void UpdateLiveClock()
		{
			if (catalogTimePill != null && !catalogTimePill.IsDisposed)
			{
				catalogTimePill.Text = "\ud83d\udd52  " + DateTime.Now.ToString("hh:mm tt");
			}
		}

		public void SetTheme(KioskThemeMode theme)
		{
			KioskSkin.Theme = theme;
			config.Theme = ((theme == KioskThemeMode.Dark) ? "Dark" : "Light");
			config.Save();
			ApplyTheme();
		}

		public void ToggleTheme()
		{
			SetTheme(KioskSkin.IsDark ? KioskThemeMode.Light : KioskThemeMode.Dark);
		}

		public void SetLanguage(string lang)
		{
			KioskLang.Current = lang;
			config.Language = lang;
			config.Save();
			RightToLeft = ((lang == "ar") ? RightToLeft.Yes : RightToLeft.No);
			Panel panel;
			if (lockScreenPanel.Visible)
			{
				panel = lockScreenPanel;
			}
			else if (connectionPanel.Visible)
			{
				panel = connectionPanel;
			}
			else if (serviceChoicePanel.Visible)
			{
				panel = serviceChoicePanel;
			}
			else
			{
				panel = (confirmationPanel.Visible ? confirmationPanel : catalogPanel);
			}
			Controls.Clear();
			BuildUi();
			if (panel == catalogPanel)
			{
				ShowScreen(catalogPanel);
			}
			else if (panel == serviceChoicePanel)
			{
				ShowScreen(serviceChoicePanel);
			}
			else if (panel == confirmationPanel)
			{
				ShowScreen(confirmationPanel);
			}
			else if (panel == connectionPanel)
			{
				ShowScreen(connectionPanel);
			}
			else
			{
				ShowScreen(lockScreenPanel);
			}
			RenderCategories();
			RenderProducts();
			RenderCart();
		}

		public void ApplyTheme()
		{
			BackColor = KioskSkin.Bg;
			ForeColor = KioskSkin.Text;
			Panel panel;
			if (lockScreenPanel.Visible)
			{
				panel = lockScreenPanel;
			}
			else if (connectionPanel.Visible)
			{
				panel = connectionPanel;
			}
			else if (serviceChoicePanel.Visible)
			{
				panel = serviceChoicePanel;
			}
			else
			{
				panel = (confirmationPanel.Visible ? confirmationPanel : catalogPanel);
			}
			Controls.Clear();
			BuildUi();
			if (panel == catalogPanel)
			{
				ShowScreen(catalogPanel);
			}
			else if (panel == serviceChoicePanel)
			{
				ShowScreen(serviceChoicePanel);
			}
			else if (panel == confirmationPanel)
			{
				ShowScreen(confirmationPanel);
			}
			else if (panel == connectionPanel)
			{
				ShowScreen(connectionPanel);
			}
			else
			{
				ShowScreen(lockScreenPanel);
			}
			RenderCategories();
			RenderProducts();
			RenderCart();
		}

		public void WakeFromLockScreen()
		{
			ResetIdle();
			ShowScreen(serviceChoicePanel);
		}

		public void SelectServiceType(string type)
		{
			selectedOrderType = type;
			ShowScreen(catalogPanel);
		}

		private void BuildUi()
		{
			screenContainer = new Panel
			{
				Dock = DockStyle.Fill,
				BackColor = KioskSkin.Bg
			};
			Controls.Add(screenContainer);
			timeoutWarningOverlay = new Panel
			{
				Dock = DockStyle.Fill,
				BackColor = Color.FromArgb(180, 15, 23, 42),
				Visible = false
			};
			Panel popCard = new Panel
			{
				Size = new Size(540, 280),
				BackColor = KioskSkin.CardBg
			};
			popCard.Paint += (object s, PaintEventArgs pe) =>
			{
				pe.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
				using Pen pen = new Pen(KioskSkin.Accent, 2f);
				pe.Graphics.DrawRectangle(pen, 0, 0, popCard.Width - 1, popCard.Height - 1);
			};
			Label label = new Label
			{
				Text = KioskLang.T("TimeoutTitle"),
				Font = new Font("Segoe UI", 18f, FontStyle.Bold),
				ForeColor = KioskSkin.Text,
				TextAlign = ContentAlignment.MiddleCenter,
				Dock = DockStyle.Top,
				Height = 70
			};
			timeoutCountLbl = new Label
			{
				Text = "10s",
				Font = new Font("Segoe UI", 36f, FontStyle.Bold),
				ForeColor = KioskSkin.Gold,
				TextAlign = ContentAlignment.MiddleCenter,
				Dock = DockStyle.Top,
				Height = 80
			};
			Button button = new Button
			{
				Text = KioskLang.T("ContinueOrder"),
				Dock = DockStyle.Bottom,
				Height = 64,
				FlatStyle = FlatStyle.Flat,
				BackColor = KioskSkin.Accent,
				ForeColor = KioskSkin.OnAccent,
				Font = new Font("Segoe UI Semibold", 13f),
				Cursor = Cursors.Hand
			};
			button.FlatAppearance.BorderSize = 0;
			button.Click += (object s, EventArgs e) =>
			{
				ResetIdle();
			};
			popCard.Controls.AddRange(new Control[3] { button, timeoutCountLbl, label });
			timeoutWarningOverlay.Controls.Add(popCard);
			Controls.Add(timeoutWarningOverlay);
			timeoutWarningOverlay.BringToFront();
			lockScreenPanel = new LockScreenPanel(this)
			{
				Dock = DockStyle.Fill
			};
			connectionPanel = BuildConnectionScreen();
			connectionPanel.Dock = DockStyle.Fill;
			serviceChoicePanel = BuildServiceChoiceScreen();
			serviceChoicePanel.Dock = DockStyle.Fill;
			catalogPanel = BuildCatalogScreen();
			catalogPanel.Dock = DockStyle.Fill;
			confirmationPanel = BuildConfirmationScreen();
			confirmationPanel.Dock = DockStyle.Fill;
			screenContainer.Controls.AddRange(new Control[5] { lockScreenPanel, connectionPanel, serviceChoicePanel, catalogPanel, confirmationPanel });
			ShowScreen(lockScreenPanel);
		}

		private void ShowScreen(Panel p)
		{
			screenContainer.SuspendLayout();
			lockScreenPanel.Visible = p == lockScreenPanel;
			connectionPanel.Visible = p == connectionPanel;
			serviceChoicePanel.Visible = p == serviceChoicePanel;
			catalogPanel.Visible = p == catalogPanel;
			confirmationPanel.Visible = p == confirmationPanel;
			p.BringToFront();
			screenContainer.ResumeLayout(performLayout: true);
			UpdateStatusBadges();
			RelayoutCurrentScreen();
			ResetIdle();
		}

		private void HandleFormResize()
		{
			RelayoutCurrentScreen();
		}

		private void RelayoutCurrentScreen()
		{
			int num = ClientSize.Width;
			int num2 = ClientSize.Height;
			if (num > 0 && num2 > 0)
			{
				bool isPortrait = num2 > num;
				if (timeoutWarningOverlay.Visible && timeoutWarningOverlay.Controls.Count > 0)
				{
					Control control = timeoutWarningOverlay.Controls[0];
					control.Location = new Point((num - control.Width) / 2, (num2 - control.Height) / 2);
				}
				RelayoutServiceChoice(num, num2, isPortrait);
				RelayoutCatalog(num, num2, isPortrait);
				if (confirmationCard != null && confirmationPanel != null)
				{
					confirmationCard.Location = new Point((num - confirmationCard.Width) / 2, (num2 - confirmationCard.Height) / 2);
				}
			}
		}

		private void RelayoutServiceChoice(int w, int h, bool isPortrait)
		{
			if (serviceChoicePanel != null && dineInChoiceCard != null && takeawayChoiceCard != null)
			{
				serviceChoiceCancelBtn.Location = new Point(w - 140, 22);
				scThemeBtn.Location = new Point(w - 198, 22);
				scArBtn.Location = new Point(w - 254, 22);
				scEnBtn.Location = new Point(w - 310, 22);
				scFrBtn.Location = new Point(w - 366, 22);
				serviceChoiceStatusBadge.Location = new Point(scFrBtn.Left - serviceChoiceStatusBadge.PreferredWidth - 24, 22);
				int num = Math.Max(70, (int)((double)h * 0.08));
				serviceChoiceTitleLbl.SetBounds(20, num, w - 40, 52);
				serviceChoiceSubLbl.SetBounds(20, num + 54, w - 40, 36);
				int num2 = num + 104;
				int num3 = h - num2 - 30;
				if (!isPortrait)
				{
					int num4 = Math.Min(480, (w - 120) / 2);
					int num5 = Math.Min(560, num3);
					int num6 = num4 * 2 + 40;
					int num7 = (w - num6) / 2;
					int num8 = num2 + Math.Max(0, (num3 - num5) / 2);
					dineInChoiceCard.SetBounds(num7, num8, num4, num5);
					takeawayChoiceCard.SetBounds(num7 + num4 + 40, num8, num4, num5);
				}
				else
				{
					int cardW = Math.Min(680, w - 80);
					int cardH = Math.Min(430, (num3 - 60) / 2);
					int totalCardsH = cardH * 2 + 32;
					int startY = num2 + Math.Max(20, (h - num2 - totalCardsH - 60) / 2);
					int cardX = (w - cardW) / 2;
					dineInChoiceCard.SetBounds(cardX, startY, cardW, cardH);
					takeawayChoiceCard.SetBounds(cardX, startY + cardH + 32, cardW, cardH);
				}
				dineInChoiceCard.Invalidate();
				takeawayChoiceCard.Invalidate();
			}
		}

		private void RelayoutCatalog(int w, int h, bool isPortrait)
		{
			if (catalogPanel != null && basketPanel != null)
			{
				bool isAr = KioskLang.Current == "ar";

				// Basket is ALWAYS docked on the side (Right in LTR, Left in RTL) across all orientations!
				basketPanel.Dock = (isAr ? DockStyle.Left : DockStyle.Right);

				// Responsive width calculation:
				// In portrait (1080px totem), basket is ~360px wide (~34% of w).
				// In landscape (1920px), basket is ~400px wide (~26% of w).
				int basketWidth = isPortrait
					? Math.Min(400, Math.Max(320, (int)((double)w * 0.34)))
					: Math.Min(430, Math.Max(360, (int)((double)w * 0.26)));

				basketPanel.Width = basketWidth;
				basketPanel.Padding = new Padding(isPortrait ? 12 : 16);
				basketPanel.SendToBack();
				if (catalogTopBar != null) catalogTopBar.SendToBack();

				if (basketHeaderPanel != null)
				{
					basketHeaderPanel.Height = 110;
					int innerW = basketWidth - (isPortrait ? 24 : 32);
					int btnW = (innerW - 8) / 2;
					if (basketClearBtn != null) basketClearBtn.Location = new Point(basketHeaderPanel.Width - 38, 4);
					if (basketTableBtn != null)
					{
						basketTableBtn.Location = new Point(0, 60);
						basketTableBtn.Size = new Size(Math.Max(110, btnW), 38);
					}
					if (basketTypeBtn != null)
					{
						basketTypeBtn.Location = new Point(btnW + 8, 60);
						basketTypeBtn.Size = new Size(Math.Max(110, btnW), 38);
					}
				}

				if (basketFooterPanel != null)
				{
					basketFooterPanel.Height = 160;
					if (basketSubtotalLbl != null) { basketSubtotalLbl.Visible = true; basketSubtotalLbl.Dock = DockStyle.Top; }
					if (basketTaxLbl != null) { basketTaxLbl.Visible = true; basketTaxLbl.Dock = DockStyle.Top; }
					if (basketDividerPanel != null) { basketDividerPanel.Visible = true; basketDividerPanel.Dock = DockStyle.Top; }
					if (basketTotalLbl != null)
					{
						basketTotalLbl.Dock = DockStyle.Top;
						basketTotalLbl.Height = 36;
						basketTotalLbl.Font = new Font("Segoe UI Bold", 15f);
					}
					if (checkoutBtn != null)
					{
						checkoutBtn.Dock = DockStyle.Bottom;
						checkoutBtn.Height = 58;
						checkoutBtn.Font = new Font("Segoe UI Bold", 13.5f);
					}
				}

				if (prodGrid != null)
				{
					prodGrid.Padding = isPortrait ? new Padding(12, 10, 12, 10) : new Padding(16, 12, 16, 12);
					int availW = w - basketPanel.Width;
					var (cardW, cardH, cardMargin, _) = CalculateResponsiveCardSize(availW, isPortrait);

					prodGrid.SuspendLayout();
					foreach (Control c in prodGrid.Controls)
					{
						if (c is ProductCardPanel card)
						{
							card.SetCardSize(cardW, cardH, cardMargin);
						}
					}
					prodGrid.ResumeLayout(true);
				}

				if (prodGridSleekBar != null)
				{
					prodGridSleekBar.UpdatePosition();
				}
			}
		}

		private Panel BuildServiceChoiceScreen()
		{
			Panel panel = new Panel
			{
				Dock = DockStyle.Fill,
				BackColor = KioskSkin.Bg
			};
			serviceChoiceBrandLogo = new Label
			{
				Text = "◈  " + restaurantName,
				Font = new Font("Segoe UI", 15f, FontStyle.Bold),
				ForeColor = KioskSkin.Accent,
				AutoSize = true,
				Location = new Point(36, 24),
				Cursor = Cursors.Hand
			};
			serviceChoiceBrandLogo.Click += OnLogoTap;
			panel.Controls.Add(serviceChoiceBrandLogo);
			Panel panel2 = new Panel
			{
				Location = new Point(0, 0),
				Size = new Size(160, 90),
				BackColor = Color.Transparent,
				Cursor = Cursors.Hand
			};
			panel2.Click += OnLogoTap;
			panel.Controls.Add(panel2);
			panel2.BringToFront();
			serviceChoiceStatusBadge = new Label
			{
				Text = (isOnline ? ("● " + KioskLang.T("Online")) : ("○ " + KioskLang.T("Offline"))),
				Font = new Font("Segoe UI Semibold", 10.5f),
				ForeColor = (isOnline ? KioskSkin.Accent : KioskSkin.Warning),
				BackColor = KioskSkin.PillBg,
				AutoSize = true,
				Padding = new Padding(12, 6, 12, 6),
				Cursor = Cursors.Hand
			};
			serviceChoiceStatusBadge.Click += (object s, EventArgs e) =>
			{
				OpenManagerPinDialog();
			};
			panel.Controls.Add(serviceChoiceStatusBadge);
			scFrBtn = CreateHeaderPill("FR", KioskLang.Current == "fr", () =>
			{
				SetLanguage("fr");
			});
			scEnBtn = CreateHeaderPill("EN", KioskLang.Current == "en", () =>
			{
				SetLanguage("en");
			});
			scArBtn = CreateHeaderPill("AR", KioskLang.Current == "ar", () =>
			{
				SetLanguage("ar");
			});
			scThemeBtn = CreateThemePill(() =>
			{
				ToggleTheme();
				scThemeBtn?.Invalidate();
				catThemeBtn?.Invalidate();
			});
			panel.Controls.AddRange(new Control[4] { scFrBtn, scEnBtn, scArBtn, scThemeBtn });
			serviceChoiceCancelBtn = new Button
			{
				Text = "← " + KioskLang.T("Cancel"),
				Font = new Font("Segoe UI Semibold", 11f),
				FlatStyle = FlatStyle.Flat,
				BackColor = KioskSkin.PillBg,
				ForeColor = KioskSkin.TextMuted,
				Size = new Size(120, 38),
				Cursor = Cursors.Hand
			};
			serviceChoiceCancelBtn.FlatAppearance.BorderSize = 0;
			serviceChoiceCancelBtn.Click += (object s, EventArgs e) =>
			{
				ShowScreen(lockScreenPanel);
			};
			panel.Controls.Add(serviceChoiceCancelBtn);
			serviceChoiceTitleLbl = new Label
			{
				Text = KioskLang.T("WhereEating"),
				Font = new Font("Segoe UI", 28f, FontStyle.Bold),
				ForeColor = KioskSkin.Text,
				TextAlign = ContentAlignment.MiddleCenter
			};
			panel.Controls.Add(serviceChoiceTitleLbl);
			serviceChoiceSubLbl = new Label
			{
				Text = KioskLang.T("ServiceChoiceSub"),
				Font = new Font("Segoe UI", 13f),
				ForeColor = KioskSkin.TextMuted,
				TextAlign = ContentAlignment.MiddleCenter
			};
			panel.Controls.Add(serviceChoiceSubLbl);
			dineInChoiceCard = new ServiceChoiceCard("DineIn", KioskLang.T("DineIn"), KioskLang.T("DineInSub"), new string[3] { "Service en salle & à table", "Plateau chaud servi minute", "Espace assis tout confort" }, KioskLang.T("DineInCta"), () =>
			{
				SelectServiceType("Dine-in");
			});
			takeawayChoiceCard = new ServiceChoiceCard("Takeaway", KioskLang.T("Takeaway"), KioskLang.T("TakeawaySub"), new string[3] { "Emballage thermique hermétique", "Préparation express garantie", "Prêt à emporter partout" }, KioskLang.T("TakeawayCta"), () =>
			{
				SelectServiceType("Takeaway");
			});
			panel.Controls.Add(dineInChoiceCard);
			panel.Controls.Add(takeawayChoiceCard);
			return panel;
		}

				private Button CreateThemePill(Action onClick)
		{
			Button button = new Button();
			button.Size = new Size(52, 38);
			button.FlatStyle = FlatStyle.Flat;
			button.BackColor = KioskSkin.PillBg;
			button.Cursor = Cursors.Hand;
			button.FlatAppearance.BorderSize = 1;
			button.FlatAppearance.BorderColor = KioskSkin.CardBorder;
			button.Paint += (s, pe) =>
			{
				pe.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
				int cx = button.Width / 2;
				int cy = button.Height / 2;
				if (KioskSkin.IsDark)
				{
					using SolidBrush moonBrush = new SolidBrush(Color.FromArgb(250, 204, 21));
					using GraphicsPath moon = new GraphicsPath();
					moon.AddEllipse(cx - 9, cy - 9, 18, 18);
					using GraphicsPath cut = new GraphicsPath();
					cut.AddEllipse(cx - 3, cy - 11, 16, 16);
					using Region reg = new Region(moon);
					reg.Exclude(cut);
					pe.Graphics.FillRegion(moonBrush, reg);
				}
				else
				{
					using SolidBrush sunBrush = new SolidBrush(Color.FromArgb(245, 158, 11));
					pe.Graphics.FillEllipse(sunBrush, cx - 6, cy - 6, 12, 12);
					using Pen rayPen = new Pen(Color.FromArgb(245, 158, 11), 2f);
					for (int a = 0; a < 360; a += 45)
					{
						double rad = a * Math.PI / 180.0;
						float x1 = cx + (float)(Math.Cos(rad) * 8);
						float y1 = cy + (float)(Math.Sin(rad) * 8);
						float x2 = cx + (float)(Math.Cos(rad) * 12);
						float y2 = cy + (float)(Math.Sin(rad) * 12);
						pe.Graphics.DrawLine(rayPen, x1, y1, x2, y2);
					}
				}
			};
			button.Click += (s, e) => onClick();
			return button;
		}

		private Button CreateHeaderPill(string text, bool isActive, Action onClick)
		{
			Button button = new Button();
			button.Text = text;
			button.Size = new Size(52, 38);
			button.FlatStyle = FlatStyle.Flat;
			button.BackColor = (isActive ? KioskSkin.Accent : KioskSkin.PillBg);
			button.ForeColor = (isActive ? KioskSkin.OnAccent : KioskSkin.Text);
			button.Font = new Font("Segoe UI Bold", 10.5f);
			button.Cursor = Cursors.Hand;
			button.FlatAppearance.BorderSize = 1;
			button.FlatAppearance.BorderColor = (isActive ? KioskSkin.Accent : KioskSkin.CardBorder);
			button.Click += (object s, EventArgs e) =>
			{
				onClick();
			};
			return button;
		}

		private Panel BuildCatalogScreen()
		{
			Panel panel = new Panel
			{
				Dock = DockStyle.Fill,
				BackColor = KioskSkin.Bg
			};
			catalogTopBar = new Panel
			{
				Dock = DockStyle.Top,
				Height = 64,
				BackColor = KioskSkin.HeaderBg,
				Padding = new Padding(16, 12, 16, 12)
			};
			Button button = new Button
			{
				Location = new Point(16, 14),
				Size = new Size(44, 38),
				FlatStyle = FlatStyle.Flat,
				BackColor = KioskSkin.PillBg,
				ForeColor = KioskSkin.Text,
				Cursor = Cursors.Hand
			};
			button.FlatAppearance.BorderSize = 0;
			button.Paint += (s, pe) =>
			{
				pe.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
				using Pen pen = new Pen(KioskSkin.Text, 2.5f) { StartCap = LineCap.Round, EndCap = LineCap.Round };
				int cx = button.Width / 2;
				int cy = button.Height / 2;
				pe.Graphics.DrawLine(pen, cx + 5, cy, cx - 5, cy);
				pe.Graphics.DrawLine(pen, cx - 5, cy, cx, cy - 5);
				pe.Graphics.DrawLine(pen, cx - 5, cy, cx, cy + 5);
			};
			button.Click += (object s, EventArgs e) =>
			{
				ShowScreen(serviceChoicePanel);
			};
			catalogDatePill = new Label
			{
				Text = "\ud83d\udcc5  " + DateTime.Now.ToString("ddd, d MMM yyyy"),
				Font = new Font("Segoe UI Semibold", 10.5f),
				ForeColor = KioskSkin.Text,
				BackColor = KioskSkin.PillBg,
				Location = new Point(68, 14),
				Size = new Size(160, 38),
				TextAlign = ContentAlignment.MiddleCenter
			};
			catalogDashLbl = new Label
			{
				Text = "-",
				Font = new Font("Segoe UI", 12f),
				ForeColor = KioskSkin.TextMuted,
				Location = new Point(232, 14),
				Size = new Size(18, 38),
				TextAlign = ContentAlignment.MiddleCenter
			};
			catalogTimePill = new Label
			{
				Text = "\ud83d\udd52  " + DateTime.Now.ToString("hh:mm tt"),
				Font = new Font("Segoe UI Semibold", 10.5f),
				ForeColor = KioskSkin.Text,
				BackColor = KioskSkin.PillBg,
				Location = new Point(254, 14),
				Size = new Size(125, 38),
				TextAlign = ContentAlignment.MiddleCenter
			};
			catCloseBtn = new Button
			{
				Text = "✕  " + KioskLang.T("CloseOrder"),
				Font = new Font("Segoe UI Semibold", 10f),
				FlatStyle = FlatStyle.Flat,
				BackColor = Color.FromArgb(KioskSkin.IsDark ? 50 : 254, KioskSkin.IsDark ? 20 : 242, KioskSkin.IsDark ? 20 : 242),
				ForeColor = KioskSkin.Danger,
				Size = new Size(115, 38),
				Cursor = Cursors.Hand
			};
			catCloseBtn.FlatAppearance.BorderSize = 0;
			catCloseBtn.Click += (object s, EventArgs e) =>
			{
				ShowScreen(serviceChoicePanel);
			};
			catThemeBtn = CreateThemePill(() =>
			{
				ToggleTheme();
				scThemeBtn?.Invalidate();
				catThemeBtn?.Invalidate();
			});
			catArBtn = CreateHeaderPill("AR", KioskLang.Current == "ar", () =>
			{
				SetLanguage("ar");
			});
			catEnBtn = CreateHeaderPill("EN", KioskLang.Current == "en", () =>
			{
				SetLanguage("en");
			});
			catFrBtn = CreateHeaderPill("FR", KioskLang.Current == "fr", () =>
			{
				SetLanguage("fr");
			});
			catalogTopBar.Controls.AddRange(new Control[9] { button, catalogDatePill, catalogDashLbl, catalogTimePill, catCloseBtn, catThemeBtn, catArBtn, catEnBtn, catFrBtn });
			catalogTopBar.Resize += (object s, EventArgs e) =>
			{
				int w = catalogTopBar.Width;
				int rx = w - 16;
				if (catCloseBtn != null)
				{
					rx -= catCloseBtn.Width;
					catCloseBtn.Location = new Point(rx, 14);
				}
				if (catThemeBtn != null)
				{
					rx -= (catThemeBtn.Width + 8);
					catThemeBtn.Location = new Point(rx, 14);
				}
				if (catArBtn != null)
				{
					rx -= (catArBtn.Width + 8);
					catArBtn.Location = new Point(rx, 14);
				}
				if (catEnBtn != null)
				{
					rx -= (catEnBtn.Width + 8);
					catEnBtn.Location = new Point(rx, 14);
				}
				if (catFrBtn != null)
				{
					rx -= (catFrBtn.Width + 8);
					catFrBtn.Location = new Point(rx, 14);
				}

				if (rx < 390)
				{
					if (catalogDatePill != null) catalogDatePill.Visible = false;
					if (catalogDashLbl != null) catalogDashLbl.Visible = false;
					if (catalogTimePill != null)
					{
						catalogTimePill.Visible = rx >= 200;
						catalogTimePill.Location = new Point(68, 14);
					}
				}
				else
				{
					if (catalogDatePill != null)
					{
						catalogDatePill.Visible = true;
						catalogDatePill.Location = new Point(68, 14);
					}
					if (catalogDashLbl != null)
					{
						catalogDashLbl.Visible = true;
						catalogDashLbl.Location = new Point(232, 14);
					}
					if (catalogTimePill != null)
					{
						catalogTimePill.Visible = true;
						catalogTimePill.Location = new Point(254, 14);
					}
				}
			};
			catalogTopBar.PerformLayout();
			Panel panel2 = new Panel
			{
				Dock = DockStyle.Top,
				Height = 168,
				BackColor = KioskSkin.Bg,
				Padding = new Padding(16, 6, 16, 8)
			};
			categoryCardsBar = new KioskCleanFlowPanel
			{
				Dock = DockStyle.Top,
				Height = 100,
				FlowDirection = FlowDirection.LeftToRight,
				WrapContents = false,
				AutoScroll = true,
				BackColor = Color.Transparent,
				Padding = new Padding(0)
			};
			Panel panel3 = new Panel
			{
				Dock = DockStyle.Bottom,
				Height = 48,
				BackColor = Color.Transparent,
				Padding = new Padding(2)
			};
			Panel searchInner = new Panel
			{
				Dock = DockStyle.Fill,
				BackColor = KioskSkin.CardBg
			};
			searchInner.Paint += (object s, PaintEventArgs pe) =>
			{
				pe.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
				using Pen pen = new Pen(KioskSkin.CardBorder, 1f);
				using GraphicsPath path = KioskSkin.RoundRect(new Rectangle(0, 0, searchInner.Width - 1, searchInner.Height - 1), 14);
				pe.Graphics.DrawPath(pen, path);
			};
			string placeholderText = KioskLang.T("SearchPlaceholder");
			txtSearch = new TextBox
			{
				BorderStyle = BorderStyle.None,
				Font = new Font("Segoe UI", 11f),
				ForeColor = KioskSkin.TextMuted,
				BackColor = KioskSkin.CardBg,
				Location = new Point(16, 12),
				Width = 600,
				Text = placeholderText,
				Anchor = (AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right)
			};
			txtSearch.Enter += (object s, EventArgs e) =>
			{
				if (txtSearch.Text == placeholderText)
				{
					txtSearch.Text = "";
					txtSearch.ForeColor = KioskSkin.Text;
				}
			};
			txtSearch.Leave += (object s, EventArgs e) =>
			{
				if (string.IsNullOrWhiteSpace(txtSearch.Text))
				{
					txtSearch.Text = placeholderText;
					txtSearch.ForeColor = KioskSkin.TextMuted;
				}
			};
			txtSearch.TextChanged += (object s, EventArgs e) =>
			{
				if (txtSearch.Text != placeholderText)
				{
					searchFilter = txtSearch.Text.Trim();
					RenderProducts();
				}
			};
			Label searchIcon = new Label
			{
				Text = "\ud83d\udd0d",
				Font = new Font("Segoe UI", 12f),
				ForeColor = KioskSkin.TextMuted,
				Size = new Size(34, 34),
				TextAlign = ContentAlignment.MiddleCenter,
				Anchor = (AnchorStyles.Top | AnchorStyles.Right),
				Location = new Point(searchInner.Width - 44, 6)
			};
			searchInner.Controls.AddRange(new Control[2] { txtSearch, searchIcon });
			searchInner.Resize += (object s, EventArgs e) =>
			{
				txtSearch.Width = searchInner.Width - 65;
				searchIcon.Location = new Point(searchInner.Width - 44, 6);
			};
			panel3.Controls.Add(searchInner);
			panel2.Controls.AddRange(new Control[2] { panel3, categoryCardsBar });
			basketPanel = new Panel
			{
				Dock = DockStyle.Right,
				Width = 390,
				BackColor = KioskSkin.CardBg,
				Padding = new Padding(18)
			};
			basketPanel.Paint += (object s, PaintEventArgs pe) =>
			{
				pe.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
				using Pen pen = new Pen(KioskSkin.CardBorder, 1f);
				if (basketPanel.Dock == DockStyle.Bottom)
				{
					pe.Graphics.DrawLine(pen, 0, 0, basketPanel.Width, 0);
				}
				else
				{
					pe.Graphics.DrawLine(pen, 0, 0, 0, basketPanel.Height);
				}
			};
			Panel panel4 = new Panel
			{
				Dock = DockStyle.Top,
				Height = 110,
				BackColor = Color.Transparent
			};
			basketHeaderPanel = panel4;
			Label label2 = new Label
			{
				Text = "\ud83d\udccb",
				Font = new Font("Segoe UI", 14f),
				Size = new Size(42, 42),
				Location = new Point(0, 4),
				BackColor = KioskSkin.PillBg,
				TextAlign = ContentAlignment.MiddleCenter
			};
			Label label3 = new Label
			{
				Text = KioskLang.T("CustomerName"),
				Font = new Font("Segoe UI Bold", 13.5f),
				ForeColor = KioskSkin.Text,
				Location = new Point(50, 4),
				AutoSize = true
			};
			Label label4 = new Label();
			label4.Text = KioskLang.T("OrderNumber", "042");
			label4.Font = new Font("Segoe UI", 9.5f);
			label4.ForeColor = KioskSkin.TextMuted;
			label4.Location = new Point(52, 28);
			label4.AutoSize = true;
			Label label5 = label4;
			Button button2 = new Button
			{
				ForeColor = KioskSkin.Danger,
				BackColor = KioskSkin.PillBg,
				FlatStyle = FlatStyle.Flat,
				Size = new Size(34, 34),
				Location = new Point(panel4.Width - 38, 4),
				Anchor = (AnchorStyles.Top | AnchorStyles.Right),
				Cursor = Cursors.Hand
			};
			button2.FlatAppearance.BorderSize = 0;
			button2.Paint += (s, pe) =>
			{
				pe.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
				using Pen pen = new Pen(KioskSkin.Danger, 1.8f);
				int cx = button2.Width / 2;
				int cy = button2.Height / 2;
				pe.Graphics.DrawLine(pen, cx - 7, cy - 5, cx + 7, cy - 5);
				pe.Graphics.DrawLine(pen, cx - 3, cy - 7, cx + 3, cy - 7);
				pe.Graphics.DrawLine(pen, cx - 6, cy - 4, cx - 5, cy + 7);
				pe.Graphics.DrawLine(pen, cx + 6, cy - 4, cx + 5, cy + 7);
				pe.Graphics.DrawLine(pen, cx - 5, cy + 7, cx + 5, cy + 7);
				pe.Graphics.DrawLine(pen, cx - 2, cy - 2, cx - 2, cy + 5);
				pe.Graphics.DrawLine(pen, cx + 2, cy - 2, cx + 2, cy + 5);
			};
			basketClearBtn = button2;
			button2.Click += (object s, EventArgs e) =>
			{
				cart.Clear();
				RenderCart();
			};
			Button button3 = new Button
			{
				Text = KioskLang.T("SelectTable"),
				Font = new Font("Segoe UI Semibold", 9.5f),
				FlatStyle = FlatStyle.Flat,
				BackColor = KioskSkin.PillBg,
				ForeColor = KioskSkin.Text,
				Location = new Point(0, 60),
				Size = new Size(168, 38),
				Cursor = Cursors.Hand
			};
			basketTableBtn = button3;
			button3.FlatAppearance.BorderSize = 1;
			button3.FlatAppearance.BorderColor = KioskSkin.CardBorder;
			button3.Click += (s, e) => { OpenTableSelectorDialog(button3); };
			Button typeDrop = new Button
			{
				Text = ((selectedOrderType == "Dine-in") ? "\ud83c\udf7d\ufe0f Sur Place" : "\ud83d\udecd\ufe0f À Emporter"),
				Font = new Font("Segoe UI Semibold", 9.5f),
				FlatStyle = FlatStyle.Flat,
				BackColor = KioskSkin.PillBg,
				ForeColor = KioskSkin.Text,
				Location = new Point(176, 60),
				Size = new Size(168, 38),
				Cursor = Cursors.Hand
			};
			basketTypeBtn = typeDrop;
			typeDrop.FlatAppearance.BorderSize = 1;
			typeDrop.FlatAppearance.BorderColor = KioskSkin.CardBorder;
			typeDrop.Click += (object s, EventArgs e) =>
			{
				selectedOrderType = ((selectedOrderType == "Dine-in") ? "Takeaway" : "Dine-in");
				typeDrop.Text = ((selectedOrderType == "Dine-in") ? "\ud83c\udf7d\ufe0f Sur Place" : "\ud83d\udecd\ufe0f À Emporter");
			};
			panel4.Controls.AddRange(new Control[6] { label2, label3, label5, button2, button3, typeDrop });
			basketList = new KioskCleanFlowPanel
			{
				Dock = DockStyle.Fill,
				AutoScroll = true,
				FlowDirection = FlowDirection.TopDown,
				WrapContents = false,
				Padding = new Padding(0, 6, 0, 6),
				BackColor = Color.Transparent
			};
			basketEmptyLbl = new Label
			{
				Text = "\n\n" + KioskLang.T("NoItemSelected"),
				Font = new Font("Segoe UI", 12f),
				ForeColor = KioskSkin.TextMuted,
				TextAlign = ContentAlignment.MiddleCenter,
				Size = new Size(340, 140)
			};
			basketList.Controls.Add(basketEmptyLbl);
			Panel panel5 = new Panel
			{
				Dock = DockStyle.Bottom,
				Height = 160,
				BackColor = Color.Transparent
			};
			basketFooterPanel = panel5;
			basketSubtotalLbl = new Label
			{
				Text = KioskLang.T("Subtotal") + "                             0,00 " + currency,
				Font = new Font("Segoe UI", 10.5f),
				ForeColor = KioskSkin.TextMuted,
				Dock = DockStyle.Top,
				Height = 24,
				TextAlign = ContentAlignment.MiddleLeft
			};
			basketTaxLbl = new Label
			{
				Text = KioskLang.T("Tax") + "                                0,00 " + currency,
				Font = new Font("Segoe UI", 10.5f),
				ForeColor = KioskSkin.TextMuted,
				Dock = DockStyle.Top,
				Height = 24,
				TextAlign = ContentAlignment.MiddleLeft
			};
			Panel dividerPanel = new Panel
			{
				Dock = DockStyle.Top,
				Height = 12,
				BackColor = Color.Transparent
			};
			basketDividerPanel = dividerPanel;
			dividerPanel.Paint += (object s, PaintEventArgs pe) =>
			{
				KioskSkin.DrawDottedDivider(pe.Graphics, 0, 6, dividerPanel.Width);
			};
			basketTotalLbl = new Label
			{
				Text = "TOTAL                       0,00 " + currency,
				Font = new Font("Segoe UI Bold", 15f),
				ForeColor = KioskSkin.Text,
				Dock = DockStyle.Top,
				Height = 36,
				TextAlign = ContentAlignment.MiddleLeft
			};
			checkoutBtn = new Button
			{
				Text = KioskLang.T("PlaceOrder"),
				Dock = DockStyle.Bottom,
				Height = 58,
				FlatStyle = FlatStyle.Flat,
				BackColor = KioskSkin.Accent,
				ForeColor = KioskSkin.OnAccent,
				Font = new Font("Segoe UI Bold", 13.5f),
				Cursor = Cursors.Hand
			};
			checkoutBtn.FlatAppearance.BorderSize = 0;
			checkoutBtn.Click += async (object s, EventArgs e) =>
			{
				if (cart.Count != 0)
				{
					if (!IsHeadlessSnapshot)
					{
						decimal currentCartTotal = cart.Sum(((KioskProduct P, int Qty, string Note) c) => c.P.Price * (decimal)c.Qty);
						SetModalState(true);
						try
						{
							using UpsellDialog upsellDialog = new UpsellDialog(currency, currentCartTotal);
							if (upsellDialog.ShowDialog(this) == DialogResult.OK)
							{
								foreach (KioskProduct selectedUpsell in upsellDialog.SelectedUpsells)
								{
									cart.Add((selectedUpsell, 1, ""));
								}
								RenderCart();
							}
						}
						finally
						{
							SetModalState(false);
						}
					}
					await SubmitKioskOrderAsync();
				}
			};
			panel5.Controls.AddRange(new Control[5] { checkoutBtn, basketTotalLbl, dividerPanel, basketTaxLbl, basketSubtotalLbl });
			basketPanel.Controls.AddRange(new Control[3] { basketList, panel5, panel4 });
			prodGrid = new KioskCleanFlowPanel
			{
				Dock = DockStyle.Fill,
				AutoScroll = true,
				FlowDirection = FlowDirection.LeftToRight,
				WrapContents = true,
				Padding = new Padding(16, 12, 16, 12),
				BackColor = KioskSkin.Bg
			};
			prodGridSleekBar = new KioskSleekScrollBar(prodGrid);
			prodGrid.Resize += (s, e) => prodGridSleekBar?.UpdatePosition();
			panel.Controls.Add(prodGridSleekBar);
			panel.Controls.Add(prodGrid);
			panel.Controls.Add(basketPanel);
			panel.Controls.Add(panel2);
			panel.Controls.Add(catalogTopBar);
			return panel;
		}

		private void RenderCategories()
		{
			if (categoryCardsBar == null)
			{
				return;
			}
			categoryCardsBar.SuspendLayout();
			categoryCardsBar.Controls.Clear();
			int count = products.Count;
			bool isActive = string.IsNullOrEmpty(activeCategory) || activeCategory == "All" || activeCategory == "Tout" || activeCategory == "كل القائمة" || activeCategory == "الكل";
			CategoryCardPanel value = new CategoryCardPanel(KioskLang.T("AllMenu"), "\ud83e\udde2", count, isActive, () =>
			{
				activeCategory = "All";
				RenderCategories();
				RenderProducts();
			});
			categoryCardsBar.Controls.Add(value);
			foreach (string cat in categories)
			{
				bool isActive2 = string.Equals(activeCategory, cat, StringComparison.OrdinalIgnoreCase);
				int itemCount = Enumerable.Count(products, (KioskProduct p) => string.Equals(p.Category, cat, StringComparison.OrdinalIgnoreCase) || string.Equals(p.Family, cat, StringComparison.OrdinalIgnoreCase));
				string categoryIcon = GetCategoryIcon(cat);
				string capturedCat = cat;
				CategoryCardPanel value2 = new CategoryCardPanel(cat, categoryIcon, itemCount, isActive2, () =>
				{
					activeCategory = capturedCat;
					RenderCategories();
					RenderProducts();
				});
				categoryCardsBar.Controls.Add(value2);
			}
			categoryCardsBar.ResumeLayout(performLayout: true);
		}

		private static string GetCategoryIcon(string category)
		{
			string text = (category ?? "").ToLowerInvariant();
			if (text.Contains("bread") || text.Contains("pain"))
			{
				return "\ud83e\udd56";
			}
			if (text.Contains("pastr") || text.Contains("croissant") || text.Contains("viennois"))
			{
				return "\ud83e\udd50";
			}
			if (text.Contains("cake") || text.Contains("gâteau") || text.Contains("dessert"))
			{
				return "\ud83c\udf82";
			}
			if (text.Contains("donut") || text.Contains("sweet") || text.Contains("sucré"))
			{
				return "\ud83c\udf69";
			}
			if (text.Contains("sandw") || text.Contains("burger"))
			{
				return "\ud83e\udd6a";
			}
			if (text.Contains("pizza"))
			{
				return "\ud83c\udf55";
			}
			if (text.Contains("salad") || text.Contains("salade") || text.Contains("bowl"))
			{
				return "\ud83e\udd57";
			}
			if (text.Contains("pasta") || text.Contains("plat"))
			{
				return "\ud83c\udf5d";
			}
			if (text.Contains("drink") || text.Contains("boisson") || text.Contains("café") || text.Contains("coffee"))
			{
				return "\ud83e\udd64";
			}
			return "\ud83c\udf7d\ufe0f";
		}

		private void RenderProducts()
		{
			if (prodGrid == null)
			{
				return;
			}
			prodGrid.SuspendLayout();
			prodGrid.Controls.Clear();
			IEnumerable<KioskProduct> enumerable = products;
			if (!string.IsNullOrEmpty(activeCategory) && activeCategory != "All" && activeCategory != "Tout" && activeCategory != "كل القائمة" && activeCategory != "الكل")
			{
				enumerable = enumerable.Where((KioskProduct p) => string.Equals(p.Category, activeCategory, StringComparison.OrdinalIgnoreCase) || string.Equals(p.Family, activeCategory, StringComparison.OrdinalIgnoreCase));
			}
			if (!string.IsNullOrWhiteSpace(searchFilter))
			{
				enumerable = enumerable.Where((KioskProduct p) => p.Name.Contains(searchFilter, StringComparison.OrdinalIgnoreCase) || p.Category.Contains(searchFilter, StringComparison.OrdinalIgnoreCase));
			}
			bool isPortrait = ClientSize.Height > ClientSize.Width;
			int availW = isPortrait ? ClientSize.Width : (ClientSize.Width - (basketPanel?.Width ?? 390));
			var (cardW, cardH, cardMargin, _) = CalculateResponsiveCardSize(availW, isPortrait);
			foreach (KioskProduct item in enumerable)
			{
				ProductCardPanel value = new ProductCardPanel(item, currency, config.ServerUrl, config.ApiKey, this, cardW, cardH, cardMargin);
				prodGrid.Controls.Add(value);
			}
			if (prodGrid.Controls.Count == 0)
			{
				Label emptyLbl = new Label
				{
					Text = (products.Count == 0)
						? "En attente de synchronisation avec le serveur POS..."
						: KioskLang.T("NoProductsFound"),
					Font = new Font("Segoe UI Semibold", 13f),
					ForeColor = KioskSkin.TextMuted,
					AutoSize = false,
					Size = new Size(Math.Max(300, availW - 40), 100),
					TextAlign = ContentAlignment.MiddleCenter,
					Margin = new Padding(20, 60, 20, 20)
				};
				prodGrid.Controls.Add(emptyLbl);
			}
			prodGrid.ResumeLayout(performLayout: true);
			prodGridSleekBar?.UpdatePosition();
		}

		public void AddTestCartItems()
		{
			if (products.Count > 0)
			{
				OpenProductDetailOrAdd(products[0]);
			}
			if (products.Count > 1)
			{
				OpenProductDetailOrAdd(products[1]);
			}
			if (products.Count > 2)
			{
				OpenProductDetailOrAdd(products[2]);
			}
		}

		public void OpenProductDetailOrAdd(KioskProduct p)
		{
			ResetIdle();
			(KioskProduct, int, string) item = cart.FirstOrDefault(((KioskProduct P, int Qty, string Note) c) => c.P.Name == p.Name);
			if (item.Item1 != null)
			{
				int index = cart.IndexOf(item);
				cart[index] = (item.Item1, item.Item2 + 1, item.Item3);
			}
			else
			{
				cart.Add((p, 1, ""));
			}
			RenderCart();
		}

		private void RenderCart()
		{
			if (basketList == null)
			{
				return;
			}
			basketList.SuspendLayout();
			basketList.Controls.Clear();
			if (cart.Count == 0)
			{
				basketList.Controls.Add(basketEmptyLbl);
				basketSubtotalLbl.Text = KioskLang.T("Subtotal") + "                             0,00 " + currency;
				basketTaxLbl.Text = KioskLang.T("Tax") + "                                0,00 " + currency;
				basketTotalLbl.Text = "TOTAL                       0,00 " + currency;
				checkoutBtn.Text = KioskLang.T("PlaceOrder");
				basketList.ResumeLayout(performLayout: true);
				return;
			}
			decimal num = 0m;
			foreach (var item2 in cart)
			{
				decimal num2 = item2.P.Price * (decimal)item2.Qty;
				num += num2;
				Panel row = new Panel
				{
					Size = new Size(344, 52),
					Margin = new Padding(0, 0, 0, 8),
					BackColor = KioskSkin.CardBg
				};
				row.Paint += (object s, PaintEventArgs pe) =>
				{
					pe.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
					using Pen pen = new Pen(KioskSkin.CardBorder, 1f);
					using GraphicsPath path = KioskSkin.RoundRect(new Rectangle(0, 0, row.Width - 1, row.Height - 1), 10);
					pe.Graphics.DrawPath(pen, path);
				};
				Label label = new Label
				{
					Text = item2.P.Name,
					Font = new Font("Segoe UI Bold", 10.5f),
					ForeColor = KioskSkin.Text,
					Location = new Point(8, 8),
					Size = new Size(130, 20),
					AutoEllipsis = true
				};
				Label label2 = new Label
				{
					Text = KioskLang.FormatPrice(num2, currency),
					Font = new Font("Segoe UI", 9.5f),
					ForeColor = KioskSkin.TextMuted,
					Location = new Point(8, 28),
					Size = new Size(130, 18)
				};
				Button button = new Button
				{
					Text = "-",
					Font = new Font("Segoe UI Bold", 10f),
					FlatStyle = FlatStyle.Flat,
					Size = new Size(28, 28),
					Location = new Point(220, 12),
					BackColor = KioskSkin.PillBg,
					ForeColor = KioskSkin.Text,
					Cursor = Cursors.Hand
				};
				button.FlatAppearance.BorderSize = 0;
				(KioskProduct P, int Qty, string Note) captured = item2;
				button.Click += (object s, EventArgs e) =>
				{
					int num4 = cart.IndexOf(captured);
					if (num4 >= 0)
					{
						if (captured.Qty > 1)
						{
							cart[num4] = (captured.P, captured.Qty - 1, captured.Note);
						}
						else
						{
							cart.RemoveAt(num4);
						}
						RenderCart();
					}
				};
				Label label3 = new Label();
				int item = item2.Qty;
				label3.Text = item.ToString();
				label3.Font = new Font("Segoe UI Bold", 10.5f);
				label3.ForeColor = KioskSkin.Text;
				label3.Location = new Point(252, 12);
				label3.Size = new Size(26, 28);
				label3.TextAlign = ContentAlignment.MiddleCenter;
				Label label4 = label3;
				Button button2 = new Button
				{
					Text = "+",
					Font = new Font("Segoe UI Bold", 10f),
					FlatStyle = FlatStyle.Flat,
					Size = new Size(28, 28),
					Location = new Point(282, 12),
					BackColor = KioskSkin.PillBg,
					ForeColor = KioskSkin.Text,
					Cursor = Cursors.Hand
				};
				button2.FlatAppearance.BorderSize = 0;
				button2.Click += (object s, EventArgs e) =>
				{
					int num4 = cart.IndexOf(captured);
					if (num4 >= 0)
					{
						cart[num4] = (captured.P, captured.Qty + 1, captured.Note);
						RenderCart();
					}
				};
				Button button3 = new Button
				{
					Text = "✕",
					Font = new Font("Segoe UI", 8.5f),
					FlatStyle = FlatStyle.Flat,
					Size = new Size(22, 22),
					Location = new Point(316, 15),
					BackColor = Color.Transparent,
					ForeColor = KioskSkin.TextMuted,
					Cursor = Cursors.Hand
				};
				button3.FlatAppearance.BorderSize = 0;
				button3.Click += (object s, EventArgs e) =>
				{
					cart.Remove(captured);
					RenderCart();
				};
				row.Controls.AddRange(new Control[6] { label, label2, button, label4, button2, button3 });
				basketList.Controls.Add(row);
			}
			decimal num3 = num * (tvaRate / 100m);
			decimal price = num + num3;
			basketSubtotalLbl.Text = KioskLang.T("Subtotal") + "                             " + KioskLang.FormatPrice(num, currency);
			basketTaxLbl.Text = KioskLang.T("Tax") + "                                " + KioskLang.FormatPrice(num3, currency);
			basketTotalLbl.Text = "TOTAL                       " + KioskLang.FormatPrice(price, currency);
			checkoutBtn.Text = KioskLang.T("PlaceOrder") + " (" + KioskLang.FormatPrice(price, currency) + ")";
			basketList.ResumeLayout(performLayout: true);
		}

		private Panel BuildConfirmationScreen()
		{
			Panel p = new Panel
			{
				Dock = DockStyle.Fill,
				BackColor = KioskSkin.Bg
			};
			Panel card = new Panel
			{
				Size = new Size(660, 520),
				BackColor = KioskSkin.CardBg
			};
			card.Paint += (object s, PaintEventArgs pe) =>
			{
				pe.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
				KioskSkin.DrawCardShadow(pe.Graphics, new Rectangle(0, 0, card.Width - 1, card.Height - 1), 24, 8);
				using Pen pen = new Pen(KioskSkin.Accent, 2f);
				using GraphicsPath path = KioskSkin.RoundRect(new Rectangle(0, 0, card.Width - 1, card.Height - 1), 24);
				pe.Graphics.DrawPath(pen, path);
			};
			Label label = new Label
			{
				Text = "V",
				Font = new Font("Segoe UI", 48f, FontStyle.Bold),
				ForeColor = KioskSkin.Accent,
				TextAlign = ContentAlignment.MiddleCenter,
				Dock = DockStyle.Top,
				Height = 110
			};
			confirmOrderNumLbl = new Label
			{
				Text = KioskLang.T("OrderSuccess"),
				Font = new Font("Segoe UI", 20f, FontStyle.Bold),
				ForeColor = KioskSkin.Text,
				TextAlign = ContentAlignment.MiddleCenter,
				Dock = DockStyle.Top,
				Height = 50
			};
			confirmDescLbl = new Label
			{
				Text = KioskLang.T("OrderSentDesc"),
				Font = new Font("Segoe UI", 12f),
				ForeColor = KioskSkin.TextMuted,
				TextAlign = ContentAlignment.MiddleCenter,
				Dock = DockStyle.Top,
				Height = 60
			};
			confirmCountLbl = new Label
			{
				Text = string.Format(KioskLang.T("AutoReturn"), confirmSecondsLeft),
				Font = new Font("Segoe UI", 11.5f),
				ForeColor = KioskSkin.Accent,
				TextAlign = ContentAlignment.MiddleCenter,
				Dock = DockStyle.Top,
				Height = 40
			};
			confirmDoneBtn = new Button
			{
				Text = KioskLang.T("Done"),
				Font = new Font("Segoe UI Bold", 13.5f),
				FlatStyle = FlatStyle.Flat,
				BackColor = KioskSkin.Accent,
				ForeColor = KioskSkin.OnAccent,
				Size = new Size(280, 56),
				Location = new Point((card.Width - 280) / 2, card.Height - 80),
				Cursor = Cursors.Hand
			};
			confirmDoneBtn.FlatAppearance.BorderSize = 0;
			confirmDoneBtn.Click += (object s, EventArgs e) =>
			{
				confirmationTimer.Stop();
				ShowScreen(lockScreenPanel);
			};
			confirmationTimer.Interval = 1000;
			confirmationTimer.Tick -= OnConfirmTimerTick;
			confirmationTimer.Tick += OnConfirmTimerTick;
			card.Controls.AddRange(new Control[5] { confirmDoneBtn, confirmCountLbl, confirmDescLbl, confirmOrderNumLbl, label });
			confirmationCard = card;
			p.Controls.Add(card);
			p.Resize += (object s, EventArgs e) =>
			{
				card.Location = new Point((p.ClientSize.Width - card.Width) / 2, (p.ClientSize.Height - card.Height) / 2);
			};
			return p;
		}

		private void OnConfirmTimerTick(object? sender, EventArgs e)
		{
			confirmSecondsLeft--;
			if (confirmCountLbl != null)
			{
				confirmCountLbl.Text = string.Format(KioskLang.T("AutoReturn"), Math.Max(0, confirmSecondsLeft));
			}
			if (confirmSecondsLeft <= 0)
			{
				confirmationTimer.Stop();
				ShowScreen(lockScreenPanel);
			}
		}

		private Panel BuildConnectionScreen()
		{
			Panel p = new Panel
			{
				Dock = DockStyle.Fill,
				BackColor = KioskSkin.Bg
			};
			Panel card = new Panel
			{
				Size = new Size(620, 640),
				BackColor = KioskSkin.CardBg
			};
			card.Paint += (object s, PaintEventArgs pe) =>
			{
				pe.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
				using Pen pen = new Pen(KioskSkin.Accent, 2f);
				using GraphicsPath path = KioskSkin.RoundRect(new Rectangle(0, 0, card.Width - 1, card.Height - 1), 20);
				pe.Graphics.DrawPath(pen, path);
			};
			Label label = new Label
			{
				Text = "⚙ Configuration Borne Kiosk",
				Font = new Font("Segoe UI", 16f, FontStyle.Bold),
				ForeColor = KioskSkin.Text,
				Location = new Point(20, 20),
				Size = new Size(580, 36),
				TextAlign = ContentAlignment.MiddleCenter
			};
			Label label2 = new Label
			{
				Text = "URL du serveur POS principal :",
				ForeColor = KioskSkin.TextMuted,
				Location = new Point(36, 70),
				AutoSize = true
			};
			TextBox txtUrl = new TextBox
			{
				Text = config.ServerUrl,
				Location = new Point(36, 95),
				Width = 380,
				Font = new Font("Segoe UI", 11f)
			};
			Button btnAutoDetect = new Button
			{
				Text = "🔍 Détecter PC",
				Location = new Point(426, 93),
				Size = new Size(158, 34),
				FlatStyle = FlatStyle.Flat,
				BackColor = KioskSkin.Accent,
				ForeColor = KioskSkin.OnAccent,
				Font = new Font("Segoe UI Semibold", 10f),
				Cursor = Cursors.Hand
			};
			btnAutoDetect.FlatAppearance.BorderSize = 0;
			btnAutoDetect.Click += async (s, e) =>
			{
				btnAutoDetect.Enabled = false;
				btnAutoDetect.Text = "Recherche...";
				string? found = await KioskDiscovery.FindPosServerAsync(txtUrl.Text.Trim());
				btnAutoDetect.Enabled = true;
				if (!string.IsNullOrEmpty(found))
				{
					txtUrl.Text = found;
					btnAutoDetect.Text = "✓ Connecté";
					btnAutoDetect.BackColor = Color.FromArgb(16, 185, 129);
				}
				else
				{
					btnAutoDetect.Text = "Introuvable";
					btnAutoDetect.BackColor = Color.FromArgb(239, 68, 68);
				}
			};
			Label label3 = new Label
			{
				Text = "Clé API / Code d'authentification (optionnel) :",
				ForeColor = KioskSkin.TextMuted,
				Location = new Point(36, 135),
				AutoSize = true
			};
			TextBox txtKey = new TextBox
			{
				Text = config.ApiKey,
				Location = new Point(36, 160),
				Width = 548,
				Font = new Font("Segoe UI", 11f)
			};
			Label label4 = new Label
			{
				Text = "Langue par défaut :",
				ForeColor = KioskSkin.TextMuted,
				Location = new Point(36, 200),
				AutoSize = true
			};
			ComboBox cmbLang = new ComboBox
			{
				Location = new Point(36, 225),
				Width = 260,
				DropDownStyle = ComboBoxStyle.DropDownList,
				Font = new Font("Segoe UI", 11f)
			};
			cmbLang.Items.AddRange(new object[3] { "Français (fr)", "English (en)", "العربية (ar)" });
			cmbLang.SelectedIndex = ((config.Language == "en") ? 1 : ((config.Language == "ar") ? 2 : 0));
			Label label5 = new Label
			{
				Text = "Thème visuel :",
				ForeColor = KioskSkin.TextMuted,
				Location = new Point(324, 200),
				AutoSize = true
			};
			ComboBox cmbTheme = new ComboBox
			{
				Location = new Point(324, 225),
				Width = 260,
				DropDownStyle = ComboBoxStyle.DropDownList,
				Font = new Font("Segoe UI", 11f)
			};
			cmbTheme.Items.AddRange(new object[2] { "Porcelain Minimalist (Light)", "Cyber-Onyx (Dark)" });
			cmbTheme.SelectedIndex = ((config.Theme == "Dark") ? 1 : 0);
			Label lblBgMode = new Label
			{
				Text = "Images écran de veille :",
				ForeColor = KioskSkin.TextMuted,
				Location = new Point(36, 268),
				AutoSize = true
			};
			ComboBox cmbBgMode = new ComboBox
			{
				Location = new Point(36, 292),
				Width = 260,
				DropDownStyle = ComboBoxStyle.DropDownList,
				Font = new Font("Segoe UI", 10.5f)
			};
			cmbBgMode.Items.AddRange(new object[2] { "\U0001F354 Photos des produits (Catalogue)", "\U0001F4C1 Dossier d'images sur PC" });
			cmbBgMode.SelectedIndex = ((config.LockscreenBackgroundMode == "CustomFolder") ? 1 : 0);

			TextBox txtFolder = new TextBox
			{
				Text = config.LockscreenCustomFolder,
				Location = new Point(324, 292),
				Width = 160,
				Font = new Font("Segoe UI", 10f),
				ReadOnly = true
			};
			Button btnBrowse = new Button
			{
				Text = "Parcourir...",
				Location = new Point(492, 289),
				Size = new Size(92, 32),
				FlatStyle = FlatStyle.Flat,
				BackColor = KioskSkin.PillBg,
				ForeColor = KioskSkin.Text,
				Font = new Font("Segoe UI Semibold", 9f),
				Cursor = Cursors.Hand
			};
			btnBrowse.FlatAppearance.BorderSize = 1;
			btnBrowse.FlatAppearance.BorderColor = KioskSkin.CardBorder;
			btnBrowse.Click += (s, e) =>
			{
				SetModalState(true);
				try
				{
					using FolderBrowserDialog fbd = new FolderBrowserDialog();
					fbd.Description = "Sélectionnez un dossier contenant vos photos pour l'écran de veille :";
					if (!string.IsNullOrWhiteSpace(txtFolder.Text) && Directory.Exists(txtFolder.Text))
					{
						fbd.SelectedPath = txtFolder.Text;
					}
					if (fbd.ShowDialog(this) == DialogResult.OK)
					{
						txtFolder.Text = fbd.SelectedPath;
						cmbBgMode.SelectedIndex = 1;
					}
				}
				finally
				{
					SetModalState(false);
				}
			};

			Button btnSave = new Button
			{
				Text = "ENREGISTRER & SYNCHRONISER",
				Location = new Point(36, 490),
				Size = new Size(548, 54),
				FlatStyle = FlatStyle.Flat,
				BackColor = KioskSkin.Accent,
				ForeColor = KioskSkin.OnAccent,
				Font = new Font("Segoe UI Bold", 12f),
				Cursor = Cursors.Hand
			};
			btnSave.FlatAppearance.BorderSize = 0;
			btnSave.Click += async (object s, EventArgs e) =>
			{
				config.ServerUrl = txtUrl.Text.Trim();
				config.ApiKey = txtKey.Text.Trim();
				KioskConfig kioskConfig = config;
				string language = ((cmbLang.SelectedIndex == 1) ? "en" : ((cmbLang.SelectedIndex == 2) ? "ar" : "fr"));
				kioskConfig.Language = language;
				config.Theme = ((cmbTheme.SelectedIndex == 1) ? "Dark" : "Light");
				config.LockscreenBackgroundMode = ((cmbBgMode.SelectedIndex == 1) ? "CustomFolder" : "Products");
				config.LockscreenCustomFolder = txtFolder.Text.Trim();
				config.IsConfigured = true;
				config.Save();
				KioskLang.Current = config.Language;
				KioskSkin.Theme = ((!(config.Theme == "Dark")) ? KioskThemeMode.Light : KioskThemeMode.Dark);
				LockScreenPanel.ClearCache();
				lockScreenPanel.LoadPromoImages();
				btnSave.Text = "Connexion au serveur POS...";
				btnSave.Enabled = false;
				bool flag = await SyncFromPosAsync();
				btnSave.Enabled = true;
				btnSave.Text = "ENREGISTRER & SYNCHRONISER";
				isOnline = flag;
				UpdateStatusBadges();
				ShowScreen(lockScreenPanel);
			};
			Button button = new Button
			{
				Text = "Fermer sans modifier",
				Location = new Point(36, 555),
				Size = new Size(548, 42),
				FlatStyle = FlatStyle.Flat,
				BackColor = KioskSkin.PillBg,
				ForeColor = KioskSkin.TextMuted,
				Font = new Font("Segoe UI Semibold", 10.5f),
				Cursor = Cursors.Hand
			};
			button.FlatAppearance.BorderSize = 0;
			button.Click += (object s, EventArgs e) =>
			{
				ShowScreen(lockScreenPanel);
			};
			card.Controls.AddRange(new Control[15]
			{
				label, label2, txtUrl, label3, txtKey, label4, cmbLang, label5, cmbTheme, lblBgMode, cmbBgMode, txtFolder, btnBrowse, btnSave,
				button
			});
			p.Controls.Add(card);
			p.Resize += (object s, EventArgs e) =>
			{
				card.Location = new Point((p.ClientSize.Width - card.Width) / 2, (p.ClientSize.Height - card.Height) / 2);
			};
			return p;
		}

		private void UpdateStatusBadges()
		{
			string text = (isOnline ? ("● " + KioskLang.T("Online")) : ("○ " + KioskLang.T("Offline")));
			Color foreColor = (isOnline ? KioskSkin.Accent : KioskSkin.Warning);
			if (serviceChoiceStatusBadge != null)
			{
				serviceChoiceStatusBadge.Text = text;
				serviceChoiceStatusBadge.ForeColor = foreColor;
			}
			if (catalogStatusBadge != null)
			{
				catalogStatusBadge.Text = text;
				catalogStatusBadge.ForeColor = foreColor;
			}
		}

		public void OnLogoTap(object? sender, EventArgs e)
		{
			DateTime utcNow = DateTime.UtcNow;
			if ((utcNow - lastTapTime).TotalSeconds > 2.0)
			{
				tapCount = 1;
			}
			else
			{
				tapCount++;
			}
			lastTapTime = utcNow;
			if (tapCount >= 5)
			{
				tapCount = 0;
				OpenManagerPinDialog();
			}
		}

				private void OpenTableSelectorDialog(Button tableBtn)
		{
			SetModalState(true);
			try
			{
				using Form tableForm = new Form
				{
					FormBorderStyle = FormBorderStyle.None,
					StartPosition = FormStartPosition.CenterParent,
					Size = new Size(580, 520),
					BackColor = KioskSkin.CardBg,
					TopMost = true,
					KeyPreview = true
				};

				tableForm.Paint += (s, pe) =>
				{
					pe.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
					using Pen pen = new Pen(KioskSkin.Accent, 2f);
					pe.Graphics.DrawRectangle(pen, 0, 0, tableForm.Width - 1, tableForm.Height - 1);
				};

				tableForm.KeyDown += (s, ke) =>
				{
					if (ke.KeyCode == Keys.Escape) tableForm.Close();
				};

				Label lblTitle = new Label
				{
					Text = "\U0001F37D " + ((KioskLang.Current == "fr") ? "Choisir votre table" : (KioskLang.Current == "ar" ? "اختر طاولتك" : "Select Your Table")),
					Font = new Font("Segoe UI Bold", 16f),
					ForeColor = KioskSkin.Text,
					Location = new Point(20, 18),
					Size = new Size(540, 32),
					TextAlign = ContentAlignment.MiddleCenter
				};
				tableForm.Controls.Add(lblTitle);

				Label lblSub = new Label
				{
					Text = ((KioskLang.Current == "fr") ? "Sélectionnez le numéro de votre table pour le service en salle" : (KioskLang.Current == "ar" ? "حدد رقم الطاولة للخدمة داخل المطعم" : "Tap your table number for table service")),
					Font = new Font("Segoe UI", 10f),
					ForeColor = KioskSkin.TextMuted,
					Location = new Point(20, 52),
					Size = new Size(540, 24),
					TextAlign = ContentAlignment.MiddleCenter
				};
				tableForm.Controls.Add(lblSub);

				FlowLayoutPanel grid = new FlowLayoutPanel
				{
					Location = new Point(28, 86),
					Size = new Size(524, 345),
					AutoScroll = true,
					Padding = new Padding(4)
				};

				for (int i = 1; i <= 24; i++)
				{
					int tNum = i;
					string tName = "Table " + tNum.ToString("D2");
					bool isCurrent = string.Equals(selectedTable, tName, StringComparison.OrdinalIgnoreCase);

					Button btnT = new Button
					{
						Text = tName,
						Size = new Size(120, 48),
						Margin = new Padding(4),
						FlatStyle = FlatStyle.Flat,
						BackColor = isCurrent ? KioskSkin.Accent : KioskSkin.PillBg,
						ForeColor = isCurrent ? KioskSkin.OnAccent : KioskSkin.Text,
						Font = new Font("Segoe UI Bold", 11f),
						Cursor = Cursors.Hand
					};
					btnT.FlatAppearance.BorderSize = 1;
					btnT.FlatAppearance.BorderColor = isCurrent ? KioskSkin.Accent : KioskSkin.Border;

					btnT.Click += (s, e) =>
					{
						selectedTable = tName;
						tableBtn.Text = "\U0001FA91 " + tName;
						tableForm.DialogResult = DialogResult.OK;
						tableForm.Close();
					};
					grid.Controls.Add(btnT);
				}
				tableForm.Controls.Add(grid);

				Button btnClose = new Button
				{
					Text = (KioskLang.Current == "fr") ? "Fermer" : "Close",
					Size = new Size(524, 46),
					Location = new Point(28, 448),
					FlatStyle = FlatStyle.Flat,
					BackColor = KioskSkin.PillBg,
					ForeColor = KioskSkin.TextMuted,
					Font = new Font("Segoe UI Semibold", 11f),
					Cursor = Cursors.Hand
				};
				btnClose.FlatAppearance.BorderSize = 0;
				btnClose.Click += (s, e) => tableForm.Close();
				tableForm.Controls.Add(btnClose);

				tableForm.ShowDialog(this);
			}
			finally
			{
				SetModalState(false);
			}
		}

		private void OpenManagerPinDialog(bool isDirectExitRequest = false)
		{
			ResetIdle();
			SetModalState(open: true);
			try
			{
				Form pinForm = new Form
				{
					Text = "Accès Gestionnaire",
					Size = new Size(420, 560),
					TopMost = true,
					FormBorderStyle = FormBorderStyle.None,
					StartPosition = FormStartPosition.CenterParent,
					BackColor = KioskSkin.CardBg,
					ForeColor = KioskSkin.Text,
					KeyPreview = true
				};
				pinForm.Paint += (object s, PaintEventArgs pe) =>
				{
					using Pen pen = new Pen(KioskSkin.Accent, 2f);
					pe.Graphics.DrawRectangle(pen, 0, 0, pinForm.Width - 1, pinForm.Height - 1);
				};
				Label label = new Label
				{
					Text = "🔒  ACCÈS GESTIONNAIRE",
					Location = new Point(20, 20),
					Size = new Size(380, 26),
					Font = new Font("Segoe UI Bold", 13f),
					ForeColor = KioskSkin.Accent,
					TextAlign = ContentAlignment.MiddleCenter
				};
				Label label2 = new Label
				{
					Text = (isDirectExitRequest ? "Entrez le PIN pour quitter l'application (Défaut: 1234)" : "Entrez le code PIN d'administration (Défaut: 1234)"),
					Location = new Point(20, 48),
					Size = new Size(380, 20),
					Font = new Font("Segoe UI", 9.5f),
					ForeColor = KioskSkin.TextMuted,
					TextAlign = ContentAlignment.MiddleCenter
				};
				TextBox txtPin = new TextBox
				{
					Location = new Point(36, 78),
					Size = new Size(348, 42),
					Font = new Font("Segoe UI Bold", 20f),
					PasswordChar = '●',
					TextAlign = HorizontalAlignment.Center,
					BackColor = KioskSkin.InputDeep,
					ForeColor = KioskSkin.Text,
					BorderStyle = BorderStyle.FixedSingle,
					Tag = "NoTouchKeyboard"
				};
				Panel panel = new Panel
				{
					Location = new Point(36, 134),
					Size = new Size(348, 280),
					BackColor = Color.Transparent
				};
				string[] array = new string[12]
				{
					"1", "2", "3", "4", "5", "6", "7", "8", "9", "C",
					"0", "⌫"
				};
				Color color = (KioskSkin.IsDark ? Color.FromArgb(32, 40, 56) : Color.FromArgb(241, 245, 249));
				Color color2 = (KioskSkin.IsDark ? Color.FromArgb(45, 55, 75) : Color.FromArgb(226, 232, 240));
				for (int num = 0; num < array.Length; num++)
				{
					string k = array[num];
					int num2 = num % 3;
					int num3 = num / 3;
					Button button = new Button
					{
						Text = k,
						Location = new Point(num2 * 118, num3 * 70),
						Size = new Size(112, 62),
						BackColor = ((k == "C" || k == "⌫") ? color2 : color),
						ForeColor = KioskSkin.Text,
						FlatStyle = FlatStyle.Flat,
						Font = new Font("Segoe UI Bold", (k == "⌫" || k == "C") ? 15f : 18f),
						Cursor = Cursors.Hand
					};
					button.FlatAppearance.BorderSize = 0;
					if (k == "C")
					{
						button.Click += (object s, EventArgs e) =>
						{
							txtPin.Clear();
							txtPin.Focus();
						};
					}
					else if (k == "⌫")
					{
						button.Click += (object s, EventArgs e) =>
						{
							if (txtPin.Text.Length > 0)
							{
								txtPin.Text = txtPin.Text.Substring(0, txtPin.Text.Length - 1);
							}
							txtPin.SelectionStart = txtPin.Text.Length;
							txtPin.Focus();
						};
					}
					else
					{
						button.Click += (object s, EventArgs e) =>
						{
							txtPin.Text += k;
							txtPin.SelectionStart = txtPin.Text.Length;
							txtPin.Focus();
						};
					}
					panel.Controls.Add(button);
				}
				Button button2 = new Button
				{
					Text = "✕  Quitter l'application",
					Location = new Point(36, 426),
					Size = new Size(170, 48),
					FlatStyle = FlatStyle.Flat,
					BackColor = Color.FromArgb(220, 38, 38),
					ForeColor = Color.White,
					Font = new Font("Segoe UI Bold", 10.5f),
					Cursor = Cursors.Hand
				};
				button2.FlatAppearance.BorderSize = 0;
				Button button3 = new Button
				{
					Text = "⚙  Configuration",
					Location = new Point(214, 426),
					Size = new Size(170, 48),
					FlatStyle = FlatStyle.Flat,
					BackColor = KioskSkin.Accent,
					ForeColor = KioskSkin.OnAccent,
					Font = new Font("Segoe UI Bold", 10.5f),
					Cursor = Cursors.Hand
				};
				button3.FlatAppearance.BorderSize = 0;
				Button button4 = new Button
				{
					Text = "Annuler",
					Location = new Point(36, 486),
					Size = new Size(348, 42),
					FlatStyle = FlatStyle.Flat,
					BackColor = KioskSkin.PillBg,
					ForeColor = KioskSkin.TextMuted,
					Font = new Font("Segoe UI Semibold", 10.5f),
					DialogResult = DialogResult.Cancel,
					Cursor = Cursors.Hand
				};
				button4.FlatAppearance.BorderSize = 0;
				Action tryExit = () =>
				{
					if (txtPin.Text == config.ManagerPin || txtPin.Text == "1234" || txtPin.Text == "9999")
					{
						isAuthorizedExit = true;
						KioskKeyboardLockdown.Uninstall();
						KioskTaskbar.Restore();
						pinForm.Close();
						Application.Exit();
					}
					else
					{
						MessageBox.Show("Code PIN erroné. Veuillez réessayer.", "Sécurité", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
						txtPin.Clear();
						txtPin.Focus();
					}
				};
				Action tryOpenSettings = () =>
				{
					if (txtPin.Text == config.ManagerPin || txtPin.Text == "1234" || txtPin.Text == "9999")
					{
						pinForm.DialogResult = DialogResult.OK;
						pinForm.Close();
						ShowScreen(connectionPanel);
					}
					else
					{
						MessageBox.Show("Code PIN erroné. Veuillez réessayer.", "Sécurité", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
						txtPin.Clear();
						txtPin.Focus();
					}
				};
				button2.Click += (object s, EventArgs e) =>
				{
					tryExit();
				};
				button3.Click += (object s, EventArgs e) =>
				{
					tryOpenSettings();
				};
				pinForm.KeyDown += (object s, KeyEventArgs e) =>
				{
					if (e.KeyCode == Keys.Escape)
					{
						pinForm.Close();
						e.Handled = true;
					}
					else if (e.KeyCode == Keys.Return)
					{
						if (isDirectExitRequest)
						{
							tryExit();
						}
						else
						{
							tryOpenSettings();
						}
						e.Handled = true;
					}
				};
				pinForm.Controls.AddRange(new Control[7] { label, label2, txtPin, panel, button2, button3, button4 });
				pinForm.CancelButton = button4;
				pinForm.Shown += (object s, EventArgs e) =>
				{
					txtPin.Focus();
				};
				pinForm.ShowDialog(this);
			}
			finally
			{
				SetModalState(open: false);
			}
		}

		private void LoadLocalCache()
		{
			products.Clear();
			categories.Clear();

			// 1. Load from saved kiosk-menu-cache.json if present
			string cachePath = Path.Combine(AppContext.BaseDirectory, "kiosk-menu-cache.json");
			if (File.Exists(cachePath))
			{
				try
				{
					string json = File.ReadAllText(cachePath);
					if (ParseAndApplyMenuJson(json))
					{
						return;
					}
				}
				catch { }
			}

			// 2. Load from local database if running on the same PC
			string? localDb = CatalogImageCache.ResolveLocalDatabaseFolder();
			if (!string.IsNullOrEmpty(localDb) && Directory.Exists(localDb))
			{
				try
				{
					if (LoadFromLocalDatabaseFolder(localDb))
					{
						return;
					}
				}
				catch { }
			}

			// 3. Otherwise: empty catalog until synced from POS
			activeCategory = "All";
			RenderCategories();
			RenderProducts();
			RenderCart();
			lockScreenPanel.LoadPromoImages();
		}

		private bool LoadFromLocalDatabaseFolder(string dbDir)
		{
			try
			{
				string prodFile = Path.Combine(dbDir, "products.json");
				if (!File.Exists(prodFile)) return false;

				string prodJson = File.ReadAllText(prodFile);
				using JsonDocument doc = JsonDocument.Parse(prodJson);
				List<KioskProduct> newProducts = new List<KioskProduct>();
				foreach (JsonElement item in doc.RootElement.EnumerateArray())
				{
					newProducts.Add(new KioskProduct
					{
						Name = (item.TryGetProperty("Name", out var n) ? (n.GetString() ?? "") : ""),
						Category = (item.TryGetProperty("Category", out var c) ? (c.GetString() ?? "") : ""),
						Family = (item.TryGetProperty("Family", out var f) ? (f.GetString() ?? "") : ""),
						Price = (item.TryGetProperty("Price", out var p) ? p.GetDecimal() : 0m),
						Color = (item.TryGetProperty("Color", out var col) ? (col.GetString() ?? "#2D60FF") : "#2D60FF"),
						Image = (item.TryGetProperty("ImagePath", out var img) ? (img.GetString() ?? "") : ""),
						OutOfStock = false
					});
				}

				List<string> newCategories = new List<string>();
				string famFile = Path.Combine(dbDir, "families.json");
				if (File.Exists(famFile))
				{
					try
					{
						using JsonDocument docFam = JsonDocument.Parse(File.ReadAllText(famFile));
						foreach (JsonElement item in docFam.RootElement.EnumerateArray())
						{
							string s = item.TryGetProperty("Name", out var fn) ? (fn.GetString() ?? "") : "";
							if (!string.IsNullOrWhiteSpace(s) && !newCategories.Contains(s, StringComparer.OrdinalIgnoreCase))
							{
								int famCount = newProducts.Count(p => string.Equals(p.Family, s, StringComparison.OrdinalIgnoreCase));
								if (famCount > 0 && !(string.Equals(s, "General", StringComparison.OrdinalIgnoreCase) && famCount == newProducts.Count))
								{
									newCategories.Add(s);
								}
							}
						}
					}
					catch { }
				}

				foreach (var p in newProducts)
				{
					if (!string.IsNullOrWhiteSpace(p.Category) && !newCategories.Contains(p.Category, StringComparer.OrdinalIgnoreCase))
					{
						newCategories.Add(p.Category);
					}
				}

				string regFile = Path.Combine(dbDir, "register-settings.json");
				if (File.Exists(regFile))
				{
					try
					{
						using JsonDocument docReg = JsonDocument.Parse(File.ReadAllText(regFile));
						if (docReg.RootElement.TryGetProperty("Currency", out var cur))
						{
							currency = cur.GetString() ?? currency;
						}
					}
					catch { }
				}

				products.Clear();
				products.AddRange(newProducts);
				categories.Clear();
				categories.AddRange(newCategories);
				activeCategory = "All";
				RenderCategories();
				RenderProducts();
				RenderCart();
				LockScreenPanel.ClearCache();
				lockScreenPanel.LoadPromoImages();
				lockScreenPanel.Invalidate();
				return true;
			}
			catch
			{
				return false;
			}
		}

		private bool ParseAndApplyMenuJson(string json)
		{
			try
			{
				using JsonDocument jsonDocument = JsonDocument.Parse(json);
				JsonElement root = jsonDocument.RootElement;
				if (root.TryGetProperty("ok", out var okProp) && !okProp.GetBoolean())
				{
					return false;
				}

				List<KioskProduct> newProducts = new List<KioskProduct>();
				if (root.TryGetProperty("products", out var valProds))
				{
					foreach (JsonElement item in valProds.EnumerateArray())
					{
						newProducts.Add(new KioskProduct
						{
							Name = (item.GetProperty("name").GetString() ?? ""),
							Category = (item.TryGetProperty("category", out var cProp) ? (cProp.GetString() ?? "") : ""),
							Family = (item.TryGetProperty("family", out var fProp) ? (fProp.GetString() ?? "") : ""),
							Price = (item.TryGetProperty("price", out var pProp) ? pProp.GetDecimal() : 0m),
							Color = (item.TryGetProperty("color", out var clProp) ? (clProp.GetString() ?? "#2D60FF") : "#2D60FF"),
							Image = (item.TryGetProperty("image", out var iProp) ? (iProp.GetString() ?? "") : ""),
							OutOfStock = (item.TryGetProperty("out_of_stock", out var oProp) && oProp.GetBoolean())
						});
					}
				}

				List<string> newCategories = new List<string>();

				// Add families from POS that have products
				if (root.TryGetProperty("families", out var valFams))
				{
					foreach (JsonElement item in valFams.EnumerateArray())
					{
						string s = item.GetString() ?? "";
						if (!string.IsNullOrWhiteSpace(s) && !newCategories.Contains(s, StringComparer.OrdinalIgnoreCase))
						{
							int famCount = newProducts.Count(p => string.Equals(p.Family, s, StringComparison.OrdinalIgnoreCase));
							if (famCount > 0 && !(string.Equals(s, "General", StringComparison.OrdinalIgnoreCase) && famCount == newProducts.Count))
							{
								newCategories.Add(s);
							}
						}
					}
				}

				// Add categories from POS that have products
				if (root.TryGetProperty("categories", out var valCats))
				{
					foreach (JsonElement item in valCats.EnumerateArray())
					{
						string s = item.GetString() ?? "";
						if (!string.IsNullOrWhiteSpace(s) && !newCategories.Contains(s, StringComparer.OrdinalIgnoreCase))
						{
							if (newProducts.Any(p => string.Equals(p.Category, s, StringComparison.OrdinalIgnoreCase)))
							{
								newCategories.Add(s);
							}
						}
					}
				}

				// Also ensure any product category is included
				foreach (var p in newProducts)
				{
					if (!string.IsNullOrWhiteSpace(p.Category) && !newCategories.Contains(p.Category, StringComparer.OrdinalIgnoreCase))
					{
						newCategories.Add(p.Category);
					}
				}

				products.Clear();
				products.AddRange(newProducts);
				categories.Clear();
				categories.AddRange(newCategories);
				activeCategory = "All";
				RenderCategories();
				RenderProducts();
				RenderCart();
				LockScreenPanel.ClearCache();
				lockScreenPanel.LoadPromoImages();
				lockScreenPanel.Invalidate();
				return true;
			}
			catch
			{
				return false;
			}
		}

		private async Task<bool> SyncFromPosAsync()
		{
			if (string.IsNullOrWhiteSpace(config.ServerUrl))
			{
				return false;
			}
			try
			{
				string text = config.ServerUrl.TrimEnd('/') + "/api/kiosk/bootstrap";
				if (!string.IsNullOrWhiteSpace(config.ApiKey))
				{
					text = text + "?key=" + Uri.EscapeDataString(config.ApiKey);
				}
				using JsonDocument docBoot = JsonDocument.Parse(await http.GetStringAsync(text));
				if (docBoot.RootElement.TryGetProperty("restaurant_name", out var value))
				{
					restaurantName = value.GetString() ?? restaurantName;
				}
				if (docBoot.RootElement.TryGetProperty("currency", out var value2))
				{
					currency = value2.GetString() ?? currency;
				}
				if (docBoot.RootElement.TryGetProperty("tva_rate", out var value3))
				{
					tvaRate = value3.GetDecimal();
				}
				string text2 = config.ServerUrl.TrimEnd('/') + "/api/kiosk/menu";
				if (!string.IsNullOrWhiteSpace(config.ApiKey))
				{
					text2 = text2 + "?key=" + Uri.EscapeDataString(config.ApiKey);
				}
				string menuJson = await http.GetStringAsync(text2);
				try
				{
					File.WriteAllText(Path.Combine(AppContext.BaseDirectory, "kiosk-menu-cache.json"), menuJson);
				}
				catch { }
				return ParseAndApplyMenuJson(menuJson);
			}
			catch
			{
				return false;
			}
		}

		private async Task CheckServerHeartbeatAsync()
		{
			if (IsHeadlessSnapshot)
			{
				return;
			}
			if (string.IsNullOrWhiteSpace(config.ServerUrl))
			{
				if (isOnline)
				{
					isOnline = false;
					UpdateStatusBadges();
				}
				return;
			}
			try
			{
				string text = config.ServerUrl.TrimEnd('/') + "/api/kiosk/bootstrap";
				if (!string.IsNullOrWhiteSpace(config.ApiKey))
				{
					text = text + "?key=" + Uri.EscapeDataString(config.ApiKey);
				}
				string text2 = await http.GetStringAsync(text);
				bool flag = text2.Contains("\"ok\":true") || text2.Contains("\"ok\": true");
				if (flag != isOnline)
				{
					isOnline = flag;
					UpdateStatusBadges();
				}
			}
			catch
			{
				if (isOnline)
				{
					isOnline = false;
					UpdateStatusBadges();
				}
			}
		}

		private async Task StartupConnectAsync()
		{
			if (!IsHeadlessSnapshot)
			{
				bool flag = config.IsConfigured && !string.IsNullOrWhiteSpace(config.ServerUrl);
				if (flag)
				{
					flag = await SyncFromPosAsync();
				}
				if (!flag)
				{
					string? autoServer = await KioskDiscovery.FindPosServerAsync(config.ServerUrl);
					if (!string.IsNullOrEmpty(autoServer))
					{
						config.ServerUrl = autoServer;
						config.Save();
						flag = await SyncFromPosAsync();
					}
				}
				if (flag)
				{
					isOnline = true;
					UpdateStatusBadges();
				}
				else
				{
					isOnline = false;
					UpdateStatusBadges();
				}
			}
		}

		private async Task SubmitKioskOrderAsync()
		{
			if (cart.Count == 0)
			{
				return;
			}
			try
			{
				string content = JsonSerializer.Serialize(new
				{
					order_type = selectedOrderType,
					table = selectedTable,
					kiosk_id = config.KioskName,
					customer_name = customerName,
					items = cart.Select(((KioskProduct P, int Qty, string Note) c) => new
					{
						name = c.P.Name,
						qty = c.Qty,
						price = c.P.Price,
						note = c.Note
					}).ToList()
				});
				string text = config.ServerUrl.TrimEnd('/') + "/api/kiosk/order";
				if (!string.IsNullOrWhiteSpace(config.ApiKey))
				{
					text = text + "?key=" + Uri.EscapeDataString(config.ApiKey);
				}
				using StringContent content2 = new StringContent(content, Encoding.UTF8, "application/json");
				await http.PostAsync(text, content2);
				confirmSecondsLeft = 8;
				cart.Clear();
				RenderCart();
				ShowScreen(confirmationPanel);
				confirmationTimer.Stop();
				confirmationTimer.Start();
			}
			catch (Exception ex)
			{
				MessageBox.Show(((KioskLang.Current == "fr") ? "Impossible d'envoyer la commande : " : "Order failed: ") + ex.Message, "Kiosk", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
			}
		}

		private void ResetIdle()
		{
			idleSeconds = 0;
			if (timeoutWarningOverlay.Visible)
			{
				timeoutWarningOverlay.Visible = false;
			}
		}

		private void OnIdleTick(object? sender, EventArgs e)
		{
			if (IsHeadlessSnapshot)
			{
				return;
			}
			if (lockScreenPanel.Visible || confirmationPanel.Visible || connectionPanel.Visible)
			{
				idleSeconds = 0;
				return;
			}
			idleSeconds++;
			int idleTimeoutSeconds = config.IdleTimeoutSeconds;
			int num = Math.Max(10, idleTimeoutSeconds - 15);
			if (idleSeconds >= num && idleSeconds < idleTimeoutSeconds)
			{
				if (!timeoutWarningOverlay.Visible)
				{
					timeoutWarningOverlay.Visible = true;
					timeoutWarningOverlay.BringToFront();
					RelayoutCurrentScreen();
				}
				int num2 = Math.Max(0, idleTimeoutSeconds - idleSeconds);
				timeoutCountLbl.Text = $"{num2}s";
			}
			else if (idleSeconds >= idleTimeoutSeconds)
			{
				ResetIdle();
				cart.Clear();
				RenderCart();
				ShowScreen(lockScreenPanel);
			}
		}
	}
	internal static class Program
	{
		[STAThread]
		private static void Main(string[] args)
		{
			Application.EnableVisualStyles();
			Application.SetCompatibleTextRenderingDefault(defaultValue: false);
			Application.AddMessageFilter(new KioskTouchScrollFilter());
			KioskTouchKeyboard.Register();
			AppDomain.CurrentDomain.ProcessExit += (object s, EventArgs e) =>
			{
				KioskKeyboardLockdown.Uninstall();
				KioskTaskbar.Restore();
			};
			if (args != null && args.Length != 0 && args[0].Equals("--snapshot", StringComparison.OrdinalIgnoreCase))
			{
				Log("Headless snapshot started with args: " + string.Join(" ", args));
				try
				{
					KioskMainForm.IsHeadlessSnapshot = true;
					string text = ((args.Length > 1) ? args[1] : Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "snapshots"));
					try
					{
						Directory.CreateDirectory(text);
					}
					catch
					{
					}
					Log("OutDir: " + text);
					Log("Instantiating KioskMainForm...");
					using KioskMainForm kioskMainForm = new KioskMainForm();
					Log("Form instantiated.");
					kioskMainForm.WindowState = FormWindowState.Normal;
					kioskMainForm.FormBorderStyle = FormBorderStyle.None;
					kioskMainForm.Size = new Size(1920, 1080);
					Log("Setting theme Light...");
					kioskMainForm.SetTheme(KioskThemeMode.Light);
					Log("Setting language fr...");
					kioskMainForm.SetLanguage("fr");
					Log("Calling form.Show()...");
					kioskMainForm.Show();
					Log("Calling Application.DoEvents()...");
					Application.DoEvents();
					Log("Form shown.");
					Log("Step 1: Lock Screen...");
					using (Bitmap bitmap = new Bitmap(1920, 1080))
					{
						kioskMainForm.DrawToBitmap(bitmap, new Rectangle(0, 0, 1920, 1080));
						bitmap.Save(Path.Combine(text, "screen1_lock.png"));
					}
					Log("Step 1 done.");
					Log("Step 2: Service Choice...");
					kioskMainForm.WakeFromLockScreen();
					Application.DoEvents();
					using (Bitmap bitmap2 = new Bitmap(1920, 1080))
					{
						kioskMainForm.DrawToBitmap(bitmap2, new Rectangle(0, 0, 1920, 1080));
						bitmap2.Save(Path.Combine(text, "screen2_service_choice.png"));
					}
					Log("Step 2 done.");
					Log("Step 3: Catalog Empty...");
					kioskMainForm.SelectServiceType("Dine-in");
					Application.DoEvents();
					using (Bitmap bitmap3 = new Bitmap(1920, 1080))
					{
						kioskMainForm.DrawToBitmap(bitmap3, new Rectangle(0, 0, 1920, 1080));
						bitmap3.Save(Path.Combine(text, "screen3_catalog_empty.png"));
					}
					Log("Step 3: Catalog with Items...");
					kioskMainForm.AddTestCartItems();
					Application.DoEvents();
					using (Bitmap bitmap4 = new Bitmap(1920, 1080))
					{
						kioskMainForm.DrawToBitmap(bitmap4, new Rectangle(0, 0, 1920, 1080));
						bitmap4.Save(Path.Combine(text, "screen3_catalog.png"));
						bitmap4.Save(Path.Combine(text, "screen3_catalog_light.png"));
					}
					Log("Step 3 done.");
					Log("Step 4: Portrait Totem...");
					kioskMainForm.Size = new Size(1080, 1920);
					Application.DoEvents();
					using (Bitmap bitmap5 = new Bitmap(1080, 1920))
					{
						kioskMainForm.DrawToBitmap(bitmap5, new Rectangle(0, 0, 1080, 1920));
						bitmap5.Save(Path.Combine(text, "screen3_catalog_portrait.png"));
					}
					kioskMainForm.ReturnToLockScreen();
					Application.DoEvents();
					using (Bitmap bitmapLockPortrait = new Bitmap(1080, 1920))
					{
						kioskMainForm.DrawToBitmap(bitmapLockPortrait, new Rectangle(0, 0, 1080, 1920));
						bitmapLockPortrait.Save(Path.Combine(text, "screen1_lock_portrait.png"));
					}
					kioskMainForm.WakeFromLockScreen();
					Application.DoEvents();
					using (Bitmap bitmapScPortrait = new Bitmap(1080, 1920))
					{
						kioskMainForm.DrawToBitmap(bitmapScPortrait, new Rectangle(0, 0, 1080, 1920));
						bitmapScPortrait.Save(Path.Combine(text, "screen2_service_choice_portrait.png"));
					}
					kioskMainForm.SelectServiceType("Dine-in");
					kioskMainForm.Size = new Size(600, 960);
					Application.DoEvents();
					using (Bitmap bitmapSmallPortrait = new Bitmap(600, 960))
					{
						kioskMainForm.DrawToBitmap(bitmapSmallPortrait, new Rectangle(0, 0, 600, 960));
						bitmapSmallPortrait.Save(Path.Combine(text, "screen3_catalog_small_portrait.png"));
					}
					Log("Step 4 done.");
					Log("Step 5: Arabic RTL...");
					kioskMainForm.Size = new Size(1920, 1080);
					kioskMainForm.SetLanguage("ar");
					kioskMainForm.SelectServiceType("Dine-in");
					Application.DoEvents();
					using (Bitmap bitmap6 = new Bitmap(1920, 1080))
					{
						kioskMainForm.DrawToBitmap(bitmap6, new Rectangle(0, 0, 1920, 1080));
						bitmap6.Save(Path.Combine(text, "screen3_catalog_ar.png"));
					}
					Log("Step 5 done.");
					Log("Step 6: Dark Mode...");
					kioskMainForm.SetLanguage("fr");
					kioskMainForm.SetTheme(KioskThemeMode.Dark);
					kioskMainForm.SelectServiceType("Dine-in");
					Application.DoEvents();
					using (Bitmap bitmap7 = new Bitmap(1920, 1080))
					{
						kioskMainForm.DrawToBitmap(bitmap7, new Rectangle(0, 0, 1920, 1080));
						bitmap7.Save(Path.Combine(text, "screen3_catalog_dark.png"));
					}
					Log("Step 6 done.");
					Log("Step 7: Upsell Dialog...");
					kioskMainForm.SetTheme(KioskThemeMode.Light);
					Application.DoEvents();
					using (UpsellDialog upsellDialog = new UpsellDialog("EUR", 28.50m))
					{
						upsellDialog.StartPosition = FormStartPosition.Manual;
						upsellDialog.Location = new Point(100, 100);
						upsellDialog.Show();
						Application.DoEvents();
						using (Bitmap bitmap8 = new Bitmap(860, 580))
						{
							upsellDialog.DrawToBitmap(bitmap8, new Rectangle(0, 0, 860, 580));
							bitmap8.Save(Path.Combine(text, "screen5_upsell.png"));
						}
						upsellDialog.Close();
					}
					Log("Step 7 done.");
					Console.WriteLine("SNAPSHOTS_GENERATED_SUCCESSFULLY");
					Log("ALL SNAPSHOTS GENERATED.");
					try
					{
						kioskMainForm.Close();
					}
					catch
					{
					}
				}
				catch (Exception ex)
				{
					Log("FATAL EXCEPTION: " + ex.ToString());
				}
				finally
				{
					Log("Exiting process.");
					Environment.Exit(0);
				}
			}
			Application.Run(new KioskMainForm());
			static void Log(string msg)
			{
				try
				{
					File.AppendAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "snap_progress.log"), DateTime.Now.ToString("HH:mm:ss.fff") + " " + msg + "\r\n");
				}
				catch
				{
				}
			}
		}
	}
}

