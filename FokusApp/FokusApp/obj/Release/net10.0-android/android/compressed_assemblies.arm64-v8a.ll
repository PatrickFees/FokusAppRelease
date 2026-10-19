; ModuleID = 'compressed_assemblies.arm64-v8a.ll'
source_filename = "compressed_assemblies.arm64-v8a.ll"
target datalayout = "e-m:e-i8:8:32-i16:16:32-i64:64-i128:128-n32:64-S128"
target triple = "aarch64-unknown-linux-android21"

%struct.CompressedAssemblyDescriptor = type {
	i32, ; uint32_t uncompressed_file_size
	i1, ; bool loaded
	i32 ; uint32_t buffer_offset
}

@compressed_assembly_count = dso_local local_unnamed_addr constant i32 163, align 4

@compressed_assembly_descriptors = dso_local local_unnamed_addr global [163 x %struct.CompressedAssemblyDescriptor] [
	%struct.CompressedAssemblyDescriptor {
		i32 15184, ; uint32_t uncompressed_file_size
		i1 false, ; bool loaded
		i32 0; uint32_t buffer_offset
	}, ; 0: Microsoft.Maui.Controls.resources
	%struct.CompressedAssemblyDescriptor {
		i32 15144, ; uint32_t uncompressed_file_size
		i1 false, ; bool loaded
		i32 15184; uint32_t buffer_offset
	}, ; 1: Microsoft.Maui.Controls.resources
	%struct.CompressedAssemblyDescriptor {
		i32 15184, ; uint32_t uncompressed_file_size
		i1 false, ; bool loaded
		i32 30328; uint32_t buffer_offset
	}, ; 2: Microsoft.Maui.Controls.resources
	%struct.CompressedAssemblyDescriptor {
		i32 15144, ; uint32_t uncompressed_file_size
		i1 false, ; bool loaded
		i32 45512; uint32_t buffer_offset
	}, ; 3: Microsoft.Maui.Controls.resources
	%struct.CompressedAssemblyDescriptor {
		i32 15184, ; uint32_t uncompressed_file_size
		i1 false, ; bool loaded
		i32 60656; uint32_t buffer_offset
	}, ; 4: Microsoft.Maui.Controls.resources
	%struct.CompressedAssemblyDescriptor {
		i32 15184, ; uint32_t uncompressed_file_size
		i1 false, ; bool loaded
		i32 75840; uint32_t buffer_offset
	}, ; 5: Microsoft.Maui.Controls.resources
	%struct.CompressedAssemblyDescriptor {
		i32 15184, ; uint32_t uncompressed_file_size
		i1 false, ; bool loaded
		i32 91024; uint32_t buffer_offset
	}, ; 6: Microsoft.Maui.Controls.resources
	%struct.CompressedAssemblyDescriptor {
		i32 15144, ; uint32_t uncompressed_file_size
		i1 false, ; bool loaded
		i32 106208; uint32_t buffer_offset
	}, ; 7: Microsoft.Maui.Controls.resources
	%struct.CompressedAssemblyDescriptor {
		i32 15144, ; uint32_t uncompressed_file_size
		i1 false, ; bool loaded
		i32 121352; uint32_t buffer_offset
	}, ; 8: Microsoft.Maui.Controls.resources
	%struct.CompressedAssemblyDescriptor {
		i32 15144, ; uint32_t uncompressed_file_size
		i1 false, ; bool loaded
		i32 136496; uint32_t buffer_offset
	}, ; 9: Microsoft.Maui.Controls.resources
	%struct.CompressedAssemblyDescriptor {
		i32 15184, ; uint32_t uncompressed_file_size
		i1 false, ; bool loaded
		i32 151640; uint32_t buffer_offset
	}, ; 10: Microsoft.Maui.Controls.resources
	%struct.CompressedAssemblyDescriptor {
		i32 15144, ; uint32_t uncompressed_file_size
		i1 false, ; bool loaded
		i32 166824; uint32_t buffer_offset
	}, ; 11: Microsoft.Maui.Controls.resources
	%struct.CompressedAssemblyDescriptor {
		i32 15184, ; uint32_t uncompressed_file_size
		i1 false, ; bool loaded
		i32 181968; uint32_t buffer_offset
	}, ; 12: Microsoft.Maui.Controls.resources
	%struct.CompressedAssemblyDescriptor {
		i32 15144, ; uint32_t uncompressed_file_size
		i1 false, ; bool loaded
		i32 197152; uint32_t buffer_offset
	}, ; 13: Microsoft.Maui.Controls.resources
	%struct.CompressedAssemblyDescriptor {
		i32 15184, ; uint32_t uncompressed_file_size
		i1 false, ; bool loaded
		i32 212296; uint32_t buffer_offset
	}, ; 14: Microsoft.Maui.Controls.resources
	%struct.CompressedAssemblyDescriptor {
		i32 15144, ; uint32_t uncompressed_file_size
		i1 false, ; bool loaded
		i32 227480; uint32_t buffer_offset
	}, ; 15: Microsoft.Maui.Controls.resources
	%struct.CompressedAssemblyDescriptor {
		i32 15144, ; uint32_t uncompressed_file_size
		i1 false, ; bool loaded
		i32 242624; uint32_t buffer_offset
	}, ; 16: Microsoft.Maui.Controls.resources
	%struct.CompressedAssemblyDescriptor {
		i32 15184, ; uint32_t uncompressed_file_size
		i1 false, ; bool loaded
		i32 257768; uint32_t buffer_offset
	}, ; 17: Microsoft.Maui.Controls.resources
	%struct.CompressedAssemblyDescriptor {
		i32 15144, ; uint32_t uncompressed_file_size
		i1 false, ; bool loaded
		i32 272952; uint32_t buffer_offset
	}, ; 18: Microsoft.Maui.Controls.resources
	%struct.CompressedAssemblyDescriptor {
		i32 15144, ; uint32_t uncompressed_file_size
		i1 false, ; bool loaded
		i32 288096; uint32_t buffer_offset
	}, ; 19: Microsoft.Maui.Controls.resources
	%struct.CompressedAssemblyDescriptor {
		i32 15144, ; uint32_t uncompressed_file_size
		i1 false, ; bool loaded
		i32 303240; uint32_t buffer_offset
	}, ; 20: Microsoft.Maui.Controls.resources
	%struct.CompressedAssemblyDescriptor {
		i32 15144, ; uint32_t uncompressed_file_size
		i1 false, ; bool loaded
		i32 318384; uint32_t buffer_offset
	}, ; 21: Microsoft.Maui.Controls.resources
	%struct.CompressedAssemblyDescriptor {
		i32 15144, ; uint32_t uncompressed_file_size
		i1 false, ; bool loaded
		i32 333528; uint32_t buffer_offset
	}, ; 22: Microsoft.Maui.Controls.resources
	%struct.CompressedAssemblyDescriptor {
		i32 15144, ; uint32_t uncompressed_file_size
		i1 false, ; bool loaded
		i32 348672; uint32_t buffer_offset
	}, ; 23: Microsoft.Maui.Controls.resources
	%struct.CompressedAssemblyDescriptor {
		i32 15144, ; uint32_t uncompressed_file_size
		i1 false, ; bool loaded
		i32 363816; uint32_t buffer_offset
	}, ; 24: Microsoft.Maui.Controls.resources
	%struct.CompressedAssemblyDescriptor {
		i32 15144, ; uint32_t uncompressed_file_size
		i1 false, ; bool loaded
		i32 378960; uint32_t buffer_offset
	}, ; 25: Microsoft.Maui.Controls.resources
	%struct.CompressedAssemblyDescriptor {
		i32 15144, ; uint32_t uncompressed_file_size
		i1 false, ; bool loaded
		i32 394104; uint32_t buffer_offset
	}, ; 26: Microsoft.Maui.Controls.resources
	%struct.CompressedAssemblyDescriptor {
		i32 15144, ; uint32_t uncompressed_file_size
		i1 false, ; bool loaded
		i32 409248; uint32_t buffer_offset
	}, ; 27: Microsoft.Maui.Controls.resources
	%struct.CompressedAssemblyDescriptor {
		i32 15144, ; uint32_t uncompressed_file_size
		i1 false, ; bool loaded
		i32 424392; uint32_t buffer_offset
	}, ; 28: Microsoft.Maui.Controls.resources
	%struct.CompressedAssemblyDescriptor {
		i32 15144, ; uint32_t uncompressed_file_size
		i1 false, ; bool loaded
		i32 439536; uint32_t buffer_offset
	}, ; 29: Microsoft.Maui.Controls.resources
	%struct.CompressedAssemblyDescriptor {
		i32 15144, ; uint32_t uncompressed_file_size
		i1 false, ; bool loaded
		i32 454680; uint32_t buffer_offset
	}, ; 30: Microsoft.Maui.Controls.resources
	%struct.CompressedAssemblyDescriptor {
		i32 15144, ; uint32_t uncompressed_file_size
		i1 false, ; bool loaded
		i32 469824; uint32_t buffer_offset
	}, ; 31: Microsoft.Maui.Controls.resources
	%struct.CompressedAssemblyDescriptor {
		i32 15144, ; uint32_t uncompressed_file_size
		i1 false, ; bool loaded
		i32 484968; uint32_t buffer_offset
	}, ; 32: Microsoft.Maui.Controls.resources
	%struct.CompressedAssemblyDescriptor {
		i32 15184, ; uint32_t uncompressed_file_size
		i1 false, ; bool loaded
		i32 500112; uint32_t buffer_offset
	}, ; 33: Microsoft.Maui.Controls.resources
	%struct.CompressedAssemblyDescriptor {
		i32 6656, ; uint32_t uncompressed_file_size
		i1 false, ; bool loaded
		i32 515296; uint32_t buffer_offset
	}, ; 34: _Microsoft.Android.Resource.Designer
	%struct.CompressedAssemblyDescriptor {
		i32 12800, ; uint32_t uncompressed_file_size
		i1 false, ; bool loaded
		i32 521952; uint32_t buffer_offset
	}, ; 35: CommunityToolkit.Mvvm
	%struct.CompressedAssemblyDescriptor {
		i32 239616, ; uint32_t uncompressed_file_size
		i1 false, ; bool loaded
		i32 534752; uint32_t buffer_offset
	}, ; 36: Microsoft.AspNetCore.Components
	%struct.CompressedAssemblyDescriptor {
		i32 58368, ; uint32_t uncompressed_file_size
		i1 false, ; bool loaded
		i32 774368; uint32_t buffer_offset
	}, ; 37: Microsoft.AspNetCore.Components.Web
	%struct.CompressedAssemblyDescriptor {
		i32 114448, ; uint32_t uncompressed_file_size
		i1 false, ; bool loaded
		i32 832736; uint32_t buffer_offset
	}, ; 38: Microsoft.AspNetCore.Components.WebView
	%struct.CompressedAssemblyDescriptor {
		i32 70952, ; uint32_t uncompressed_file_size
		i1 false, ; bool loaded
		i32 947184; uint32_t buffer_offset
	}, ; 39: Microsoft.AspNetCore.Components.WebView.Maui
	%struct.CompressedAssemblyDescriptor {
		i32 177464, ; uint32_t uncompressed_file_size
		i1 false, ; bool loaded
		i32 1018136; uint32_t buffer_offset
	}, ; 40: Microsoft.Data.Sqlite
	%struct.CompressedAssemblyDescriptor {
		i32 2828600, ; uint32_t uncompressed_file_size
		i1 false, ; bool loaded
		i32 1195600; uint32_t buffer_offset
	}, ; 41: Microsoft.EntityFrameworkCore
	%struct.CompressedAssemblyDescriptor {
		i32 14336, ; uint32_t uncompressed_file_size
		i1 false, ; bool loaded
		i32 4024200; uint32_t buffer_offset
	}, ; 42: Microsoft.EntityFrameworkCore.Abstractions
	%struct.CompressedAssemblyDescriptor {
		i32 2194784, ; uint32_t uncompressed_file_size
		i1 false, ; bool loaded
		i32 4038536; uint32_t buffer_offset
	}, ; 43: Microsoft.EntityFrameworkCore.Relational
	%struct.CompressedAssemblyDescriptor {
		i32 311608, ; uint32_t uncompressed_file_size
		i1 false, ; bool loaded
		i32 6233320; uint32_t buffer_offset
	}, ; 44: Microsoft.EntityFrameworkCore.Sqlite
	%struct.CompressedAssemblyDescriptor {
		i32 10752, ; uint32_t uncompressed_file_size
		i1 false, ; bool loaded
		i32 6544928; uint32_t buffer_offset
	}, ; 45: Microsoft.Extensions.Caching.Abstractions
	%struct.CompressedAssemblyDescriptor {
		i32 25600, ; uint32_t uncompressed_file_size
		i1 false, ; bool loaded
		i32 6555680; uint32_t buffer_offset
	}, ; 46: Microsoft.Extensions.Caching.Memory
	%struct.CompressedAssemblyDescriptor {
		i32 15872, ; uint32_t uncompressed_file_size
		i1 false, ; bool loaded
		i32 6581280; uint32_t buffer_offset
	}, ; 47: Microsoft.Extensions.Configuration
	%struct.CompressedAssemblyDescriptor {
		i32 6656, ; uint32_t uncompressed_file_size
		i1 false, ; bool loaded
		i32 6597152; uint32_t buffer_offset
	}, ; 48: Microsoft.Extensions.Configuration.Abstractions
	%struct.CompressedAssemblyDescriptor {
		i32 47104, ; uint32_t uncompressed_file_size
		i1 false, ; bool loaded
		i32 6603808; uint32_t buffer_offset
	}, ; 49: Microsoft.Extensions.DependencyInjection
	%struct.CompressedAssemblyDescriptor {
		i32 33792, ; uint32_t uncompressed_file_size
		i1 false, ; bool loaded
		i32 6650912; uint32_t buffer_offset
	}, ; 50: Microsoft.Extensions.DependencyInjection.Abstractions
	%struct.CompressedAssemblyDescriptor {
		i32 35840, ; uint32_t uncompressed_file_size
		i1 false, ; bool loaded
		i32 6684704; uint32_t buffer_offset
	}, ; 51: Microsoft.Extensions.DependencyModel
	%struct.CompressedAssemblyDescriptor {
		i32 8192, ; uint32_t uncompressed_file_size
		i1 false, ; bool loaded
		i32 6720544; uint32_t buffer_offset
	}, ; 52: Microsoft.Extensions.Diagnostics.Abstractions
	%struct.CompressedAssemblyDescriptor {
		i32 9216, ; uint32_t uncompressed_file_size
		i1 false, ; bool loaded
		i32 6728736; uint32_t buffer_offset
	}, ; 53: Microsoft.Extensions.FileProviders.Abstractions
	%struct.CompressedAssemblyDescriptor {
		i32 7680, ; uint32_t uncompressed_file_size
		i1 false, ; bool loaded
		i32 6737952; uint32_t buffer_offset
	}, ; 54: Microsoft.Extensions.FileProviders.Composite
	%struct.CompressedAssemblyDescriptor {
		i32 19968, ; uint32_t uncompressed_file_size
		i1 false, ; bool loaded
		i32 6745632; uint32_t buffer_offset
	}, ; 55: Microsoft.Extensions.FileProviders.Physical
	%struct.CompressedAssemblyDescriptor {
		i32 27648, ; uint32_t uncompressed_file_size
		i1 false, ; bool loaded
		i32 6765600; uint32_t buffer_offset
	}, ; 56: Microsoft.Extensions.FileSystemGlobbing
	%struct.CompressedAssemblyDescriptor {
		i32 6144, ; uint32_t uncompressed_file_size
		i1 false, ; bool loaded
		i32 6793248; uint32_t buffer_offset
	}, ; 57: Microsoft.Extensions.Hosting.Abstractions
	%struct.CompressedAssemblyDescriptor {
		i32 32056, ; uint32_t uncompressed_file_size
		i1 false, ; bool loaded
		i32 6799392; uint32_t buffer_offset
	}, ; 58: Microsoft.Extensions.Localization
	%struct.CompressedAssemblyDescriptor {
		i32 7680, ; uint32_t uncompressed_file_size
		i1 false, ; bool loaded
		i32 6831448; uint32_t buffer_offset
	}, ; 59: Microsoft.Extensions.Localization.Abstractions
	%struct.CompressedAssemblyDescriptor {
		i32 19968, ; uint32_t uncompressed_file_size
		i1 false, ; bool loaded
		i32 6839128; uint32_t buffer_offset
	}, ; 60: Microsoft.Extensions.Logging
	%struct.CompressedAssemblyDescriptor {
		i32 38912, ; uint32_t uncompressed_file_size
		i1 false, ; bool loaded
		i32 6859096; uint32_t buffer_offset
	}, ; 61: Microsoft.Extensions.Logging.Abstractions
	%struct.CompressedAssemblyDescriptor {
		i32 17920, ; uint32_t uncompressed_file_size
		i1 false, ; bool loaded
		i32 6898008; uint32_t buffer_offset
	}, ; 62: Microsoft.Extensions.Options
	%struct.CompressedAssemblyDescriptor {
		i32 13824, ; uint32_t uncompressed_file_size
		i1 false, ; bool loaded
		i32 6915928; uint32_t buffer_offset
	}, ; 63: Microsoft.Extensions.Primitives
	%struct.CompressedAssemblyDescriptor {
		i32 43008, ; uint32_t uncompressed_file_size
		i1 false, ; bool loaded
		i32 6929752; uint32_t buffer_offset
	}, ; 64: Microsoft.JSInterop
	%struct.CompressedAssemblyDescriptor {
		i32 1972008, ; uint32_t uncompressed_file_size
		i1 false, ; bool loaded
		i32 6972760; uint32_t buffer_offset
	}, ; 65: Microsoft.Maui.Controls
	%struct.CompressedAssemblyDescriptor {
		i32 135464, ; uint32_t uncompressed_file_size
		i1 false, ; bool loaded
		i32 8944768; uint32_t buffer_offset
	}, ; 66: Microsoft.Maui.Controls.Xaml
	%struct.CompressedAssemblyDescriptor {
		i32 967464, ; uint32_t uncompressed_file_size
		i1 false, ; bool loaded
		i32 9080232; uint32_t buffer_offset
	}, ; 67: Microsoft.Maui
	%struct.CompressedAssemblyDescriptor {
		i32 80896, ; uint32_t uncompressed_file_size
		i1 false, ; bool loaded
		i32 10047696; uint32_t buffer_offset
	}, ; 68: Microsoft.Maui.Essentials
	%struct.CompressedAssemblyDescriptor {
		i32 209744, ; uint32_t uncompressed_file_size
		i1 false, ; bool loaded
		i32 10128592; uint32_t buffer_offset
	}, ; 69: Microsoft.Maui.Graphics
	%struct.CompressedAssemblyDescriptor {
		i32 52736, ; uint32_t uncompressed_file_size
		i1 false, ; bool loaded
		i32 10338336; uint32_t buffer_offset
	}, ; 70: Plugin.Maui.Audio
	%struct.CompressedAssemblyDescriptor {
		i32 5632, ; uint32_t uncompressed_file_size
		i1 false, ; bool loaded
		i32 10391072; uint32_t buffer_offset
	}, ; 71: SQLitePCLRaw.batteries_v2
	%struct.CompressedAssemblyDescriptor {
		i32 51200, ; uint32_t uncompressed_file_size
		i1 false, ; bool loaded
		i32 10396704; uint32_t buffer_offset
	}, ; 72: SQLitePCLRaw.core
	%struct.CompressedAssemblyDescriptor {
		i32 5120, ; uint32_t uncompressed_file_size
		i1 false, ; bool loaded
		i32 10447904; uint32_t buffer_offset
	}, ; 73: SQLitePCLRaw.lib.e_sqlite3.android
	%struct.CompressedAssemblyDescriptor {
		i32 36864, ; uint32_t uncompressed_file_size
		i1 false, ; bool loaded
		i32 10453024; uint32_t buffer_offset
	}, ; 74: SQLitePCLRaw.provider.e_sqlite3
	%struct.CompressedAssemblyDescriptor {
		i32 73216, ; uint32_t uncompressed_file_size
		i1 false, ; bool loaded
		i32 10489888; uint32_t buffer_offset
	}, ; 75: Xamarin.AndroidX.Activity
	%struct.CompressedAssemblyDescriptor {
		i32 582656, ; uint32_t uncompressed_file_size
		i1 false, ; bool loaded
		i32 10563104; uint32_t buffer_offset
	}, ; 76: Xamarin.AndroidX.AppCompat
	%struct.CompressedAssemblyDescriptor {
		i32 17408, ; uint32_t uncompressed_file_size
		i1 false, ; bool loaded
		i32 11145760; uint32_t buffer_offset
	}, ; 77: Xamarin.AndroidX.AppCompat.AppCompatResources
	%struct.CompressedAssemblyDescriptor {
		i32 18944, ; uint32_t uncompressed_file_size
		i1 false, ; bool loaded
		i32 11163168; uint32_t buffer_offset
	}, ; 78: Xamarin.AndroidX.CardView
	%struct.CompressedAssemblyDescriptor {
		i32 22528, ; uint32_t uncompressed_file_size
		i1 false, ; bool loaded
		i32 11182112; uint32_t buffer_offset
	}, ; 79: Xamarin.AndroidX.Collection.Jvm
	%struct.CompressedAssemblyDescriptor {
		i32 78336, ; uint32_t uncompressed_file_size
		i1 false, ; bool loaded
		i32 11204640; uint32_t buffer_offset
	}, ; 80: Xamarin.AndroidX.CoordinatorLayout
	%struct.CompressedAssemblyDescriptor {
		i32 780288, ; uint32_t uncompressed_file_size
		i1 false, ; bool loaded
		i32 11282976; uint32_t buffer_offset
	}, ; 81: Xamarin.AndroidX.Core
	%struct.CompressedAssemblyDescriptor {
		i32 26624, ; uint32_t uncompressed_file_size
		i1 false, ; bool loaded
		i32 12063264; uint32_t buffer_offset
	}, ; 82: Xamarin.AndroidX.CursorAdapter
	%struct.CompressedAssemblyDescriptor {
		i32 9728, ; uint32_t uncompressed_file_size
		i1 false, ; bool loaded
		i32 12089888; uint32_t buffer_offset
	}, ; 83: Xamarin.AndroidX.CustomView
	%struct.CompressedAssemblyDescriptor {
		i32 46592, ; uint32_t uncompressed_file_size
		i1 false, ; bool loaded
		i32 12099616; uint32_t buffer_offset
	}, ; 84: Xamarin.AndroidX.DrawerLayout
	%struct.CompressedAssemblyDescriptor {
		i32 233984, ; uint32_t uncompressed_file_size
		i1 false, ; bool loaded
		i32 12146208; uint32_t buffer_offset
	}, ; 85: Xamarin.AndroidX.Fragment
	%struct.CompressedAssemblyDescriptor {
		i32 23552, ; uint32_t uncompressed_file_size
		i1 false, ; bool loaded
		i32 12380192; uint32_t buffer_offset
	}, ; 86: Xamarin.AndroidX.Lifecycle.Common.Jvm
	%struct.CompressedAssemblyDescriptor {
		i32 18944, ; uint32_t uncompressed_file_size
		i1 false, ; bool loaded
		i32 12403744; uint32_t buffer_offset
	}, ; 87: Xamarin.AndroidX.Lifecycle.LiveData.Core
	%struct.CompressedAssemblyDescriptor {
		i32 32768, ; uint32_t uncompressed_file_size
		i1 false, ; bool loaded
		i32 12422688; uint32_t buffer_offset
	}, ; 88: Xamarin.AndroidX.Lifecycle.ViewModel.Android
	%struct.CompressedAssemblyDescriptor {
		i32 13824, ; uint32_t uncompressed_file_size
		i1 false, ; bool loaded
		i32 12455456; uint32_t buffer_offset
	}, ; 89: Xamarin.AndroidX.Lifecycle.ViewModelSavedState.Android
	%struct.CompressedAssemblyDescriptor {
		i32 39424, ; uint32_t uncompressed_file_size
		i1 false, ; bool loaded
		i32 12469280; uint32_t buffer_offset
	}, ; 90: Xamarin.AndroidX.Loader
	%struct.CompressedAssemblyDescriptor {
		i32 92672, ; uint32_t uncompressed_file_size
		i1 false, ; bool loaded
		i32 12508704; uint32_t buffer_offset
	}, ; 91: Xamarin.AndroidX.Navigation.Common.Android
	%struct.CompressedAssemblyDescriptor {
		i32 19456, ; uint32_t uncompressed_file_size
		i1 false, ; bool loaded
		i32 12601376; uint32_t buffer_offset
	}, ; 92: Xamarin.AndroidX.Navigation.Fragment
	%struct.CompressedAssemblyDescriptor {
		i32 65024, ; uint32_t uncompressed_file_size
		i1 false, ; bool loaded
		i32 12620832; uint32_t buffer_offset
	}, ; 93: Xamarin.AndroidX.Navigation.Runtime.Android
	%struct.CompressedAssemblyDescriptor {
		i32 27136, ; uint32_t uncompressed_file_size
		i1 false, ; bool loaded
		i32 12685856; uint32_t buffer_offset
	}, ; 94: Xamarin.AndroidX.Navigation.UI
	%struct.CompressedAssemblyDescriptor {
		i32 454144, ; uint32_t uncompressed_file_size
		i1 false, ; bool loaded
		i32 12712992; uint32_t buffer_offset
	}, ; 95: Xamarin.AndroidX.RecyclerView
	%struct.CompressedAssemblyDescriptor {
		i32 12288, ; uint32_t uncompressed_file_size
		i1 false, ; bool loaded
		i32 13167136; uint32_t buffer_offset
	}, ; 96: Xamarin.AndroidX.SavedState.SavedState.Android
	%struct.CompressedAssemblyDescriptor {
		i32 41472, ; uint32_t uncompressed_file_size
		i1 false, ; bool loaded
		i32 13179424; uint32_t buffer_offset
	}, ; 97: Xamarin.AndroidX.SwipeRefreshLayout
	%struct.CompressedAssemblyDescriptor {
		i32 9728, ; uint32_t uncompressed_file_size
		i1 false, ; bool loaded
		i32 13220896; uint32_t buffer_offset
	}, ; 98: Xamarin.AndroidX.VersionedParcelable
	%struct.CompressedAssemblyDescriptor {
		i32 62464, ; uint32_t uncompressed_file_size
		i1 false, ; bool loaded
		i32 13230624; uint32_t buffer_offset
	}, ; 99: Xamarin.AndroidX.ViewPager
	%struct.CompressedAssemblyDescriptor {
		i32 39936, ; uint32_t uncompressed_file_size
		i1 false, ; bool loaded
		i32 13293088; uint32_t buffer_offset
	}, ; 100: Xamarin.AndroidX.ViewPager2
	%struct.CompressedAssemblyDescriptor {
		i32 979968, ; uint32_t uncompressed_file_size
		i1 false, ; bool loaded
		i32 13333024; uint32_t buffer_offset
	}, ; 101: Xamarin.Google.Android.Material
	%struct.CompressedAssemblyDescriptor {
		i32 90624, ; uint32_t uncompressed_file_size
		i1 false, ; bool loaded
		i32 14312992; uint32_t buffer_offset
	}, ; 102: Xamarin.Kotlin.StdLib
	%struct.CompressedAssemblyDescriptor {
		i32 28672, ; uint32_t uncompressed_file_size
		i1 false, ; bool loaded
		i32 14403616; uint32_t buffer_offset
	}, ; 103: Xamarin.KotlinX.Coroutines.Core.Jvm
	%struct.CompressedAssemblyDescriptor {
		i32 91648, ; uint32_t uncompressed_file_size
		i1 false, ; bool loaded
		i32 14432288; uint32_t buffer_offset
	}, ; 104: Xamarin.KotlinX.Serialization.Core.Jvm
	%struct.CompressedAssemblyDescriptor {
		i32 157696, ; uint32_t uncompressed_file_size
		i1 false, ; bool loaded
		i32 14523936; uint32_t buffer_offset
	}, ; 105: FokusApp.Core
	%struct.CompressedAssemblyDescriptor {
		i32 44544, ; uint32_t uncompressed_file_size
		i1 false, ; bool loaded
		i32 14681632; uint32_t buffer_offset
	}, ; 106: FokusApp.Migrations
	%struct.CompressedAssemblyDescriptor {
		i32 102912, ; uint32_t uncompressed_file_size
		i1 false, ; bool loaded
		i32 14726176; uint32_t buffer_offset
	}, ; 107: FokusApp.Presentation
	%struct.CompressedAssemblyDescriptor {
		i32 194048, ; uint32_t uncompressed_file_size
		i1 false, ; bool loaded
		i32 14829088; uint32_t buffer_offset
	}, ; 108: FokusApp
	%struct.CompressedAssemblyDescriptor {
		i32 28672, ; uint32_t uncompressed_file_size
		i1 false, ; bool loaded
		i32 15023136; uint32_t buffer_offset
	}, ; 109: System.Collections.Concurrent
	%struct.CompressedAssemblyDescriptor {
		i32 77824, ; uint32_t uncompressed_file_size
		i1 false, ; bool loaded
		i32 15051808; uint32_t buffer_offset
	}, ; 110: System.Collections.Immutable
	%struct.CompressedAssemblyDescriptor {
		i32 18944, ; uint32_t uncompressed_file_size
		i1 false, ; bool loaded
		i32 15129632; uint32_t buffer_offset
	}, ; 111: System.Collections.NonGeneric
	%struct.CompressedAssemblyDescriptor {
		i32 12288, ; uint32_t uncompressed_file_size
		i1 false, ; bool loaded
		i32 15148576; uint32_t buffer_offset
	}, ; 112: System.Collections.Specialized
	%struct.CompressedAssemblyDescriptor {
		i32 60416, ; uint32_t uncompressed_file_size
		i1 false, ; bool loaded
		i32 15160864; uint32_t buffer_offset
	}, ; 113: System.Collections
	%struct.CompressedAssemblyDescriptor {
		i32 6656, ; uint32_t uncompressed_file_size
		i1 false, ; bool loaded
		i32 15221280; uint32_t buffer_offset
	}, ; 114: System.ComponentModel.Annotations
	%struct.CompressedAssemblyDescriptor {
		i32 15360, ; uint32_t uncompressed_file_size
		i1 false, ; bool loaded
		i32 15227936; uint32_t buffer_offset
	}, ; 115: System.ComponentModel.Primitives
	%struct.CompressedAssemblyDescriptor {
		i32 143872, ; uint32_t uncompressed_file_size
		i1 false, ; bool loaded
		i32 15243296; uint32_t buffer_offset
	}, ; 116: System.ComponentModel.TypeConverter
	%struct.CompressedAssemblyDescriptor {
		i32 5120, ; uint32_t uncompressed_file_size
		i1 false, ; bool loaded
		i32 15387168; uint32_t buffer_offset
	}, ; 117: System.ComponentModel
	%struct.CompressedAssemblyDescriptor {
		i32 12288, ; uint32_t uncompressed_file_size
		i1 false, ; bool loaded
		i32 15392288; uint32_t buffer_offset
	}, ; 118: System.Console
	%struct.CompressedAssemblyDescriptor {
		i32 562688, ; uint32_t uncompressed_file_size
		i1 false, ; bool loaded
		i32 15404576; uint32_t buffer_offset
	}, ; 119: System.Data.Common
	%struct.CompressedAssemblyDescriptor {
		i32 52224, ; uint32_t uncompressed_file_size
		i1 false, ; bool loaded
		i32 15967264; uint32_t buffer_offset
	}, ; 120: System.Diagnostics.DiagnosticSource
	%struct.CompressedAssemblyDescriptor {
		i32 22016, ; uint32_t uncompressed_file_size
		i1 false, ; bool loaded
		i32 16019488; uint32_t buffer_offset
	}, ; 121: System.Diagnostics.TraceSource
	%struct.CompressedAssemblyDescriptor {
		i32 5120, ; uint32_t uncompressed_file_size
		i1 false, ; bool loaded
		i32 16041504; uint32_t buffer_offset
	}, ; 122: System.Diagnostics.Tracing
	%struct.CompressedAssemblyDescriptor {
		i32 36864, ; uint32_t uncompressed_file_size
		i1 false, ; bool loaded
		i32 16046624; uint32_t buffer_offset
	}, ; 123: System.Drawing.Primitives
	%struct.CompressedAssemblyDescriptor {
		i32 5120, ; uint32_t uncompressed_file_size
		i1 false, ; bool loaded
		i32 16083488; uint32_t buffer_offset
	}, ; 124: System.Drawing
	%struct.CompressedAssemblyDescriptor {
		i32 60416, ; uint32_t uncompressed_file_size
		i1 false, ; bool loaded
		i32 16088608; uint32_t buffer_offset
	}, ; 125: System.Formats.Asn1
	%struct.CompressedAssemblyDescriptor {
		i32 22016, ; uint32_t uncompressed_file_size
		i1 false, ; bool loaded
		i32 16149024; uint32_t buffer_offset
	}, ; 126: System.IO.Compression.Brotli
	%struct.CompressedAssemblyDescriptor {
		i32 32256, ; uint32_t uncompressed_file_size
		i1 false, ; bool loaded
		i32 16171040; uint32_t buffer_offset
	}, ; 127: System.IO.Compression
	%struct.CompressedAssemblyDescriptor {
		i32 30720, ; uint32_t uncompressed_file_size
		i1 false, ; bool loaded
		i32 16203296; uint32_t buffer_offset
	}, ; 128: System.IO.FileSystem.Watcher
	%struct.CompressedAssemblyDescriptor {
		i32 6144, ; uint32_t uncompressed_file_size
		i1 false, ; bool loaded
		i32 16234016; uint32_t buffer_offset
	}, ; 129: System.IO.Pipelines
	%struct.CompressedAssemblyDescriptor {
		i32 447488, ; uint32_t uncompressed_file_size
		i1 false, ; bool loaded
		i32 16240160; uint32_t buffer_offset
	}, ; 130: System.Linq.Expressions
	%struct.CompressedAssemblyDescriptor {
		i32 55808, ; uint32_t uncompressed_file_size
		i1 false, ; bool loaded
		i32 16687648; uint32_t buffer_offset
	}, ; 131: System.Linq.Queryable
	%struct.CompressedAssemblyDescriptor {
		i32 161792, ; uint32_t uncompressed_file_size
		i1 false, ; bool loaded
		i32 16743456; uint32_t buffer_offset
	}, ; 132: System.Linq
	%struct.CompressedAssemblyDescriptor {
		i32 16384, ; uint32_t uncompressed_file_size
		i1 false, ; bool loaded
		i32 16905248; uint32_t buffer_offset
	}, ; 133: System.Memory
	%struct.CompressedAssemblyDescriptor {
		i32 125952, ; uint32_t uncompressed_file_size
		i1 false, ; bool loaded
		i32 16921632; uint32_t buffer_offset
	}, ; 134: System.Net.Http
	%struct.CompressedAssemblyDescriptor {
		i32 9216, ; uint32_t uncompressed_file_size
		i1 false, ; bool loaded
		i32 17047584; uint32_t buffer_offset
	}, ; 135: System.Net.NetworkInformation
	%struct.CompressedAssemblyDescriptor {
		i32 58368, ; uint32_t uncompressed_file_size
		i1 false, ; bool loaded
		i32 17056800; uint32_t buffer_offset
	}, ; 136: System.Net.Primitives
	%struct.CompressedAssemblyDescriptor {
		i32 7680, ; uint32_t uncompressed_file_size
		i1 false, ; bool loaded
		i32 17115168; uint32_t buffer_offset
	}, ; 137: System.Net.Requests
	%struct.CompressedAssemblyDescriptor {
		i32 5120, ; uint32_t uncompressed_file_size
		i1 false, ; bool loaded
		i32 17122848; uint32_t buffer_offset
	}, ; 138: System.Numerics.Vectors
	%struct.CompressedAssemblyDescriptor {
		i32 18432, ; uint32_t uncompressed_file_size
		i1 false, ; bool loaded
		i32 17127968; uint32_t buffer_offset
	}, ; 139: System.ObjectModel
	%struct.CompressedAssemblyDescriptor {
		i32 74752, ; uint32_t uncompressed_file_size
		i1 false, ; bool loaded
		i32 17146400; uint32_t buffer_offset
	}, ; 140: System.Private.Uri
	%struct.CompressedAssemblyDescriptor {
		i32 1350656, ; uint32_t uncompressed_file_size
		i1 false, ; bool loaded
		i32 17221152; uint32_t buffer_offset
	}, ; 141: System.Private.Xml
	%struct.CompressedAssemblyDescriptor {
		i32 5120, ; uint32_t uncompressed_file_size
		i1 false, ; bool loaded
		i32 18571808; uint32_t buffer_offset
	}, ; 142: System.Runtime.InteropServices.RuntimeInformation
	%struct.CompressedAssemblyDescriptor {
		i32 9216, ; uint32_t uncompressed_file_size
		i1 false, ; bool loaded
		i32 18576928; uint32_t buffer_offset
	}, ; 143: System.Runtime.InteropServices
	%struct.CompressedAssemblyDescriptor {
		i32 5120, ; uint32_t uncompressed_file_size
		i1 false, ; bool loaded
		i32 18586144; uint32_t buffer_offset
	}, ; 144: System.Runtime.Loader
	%struct.CompressedAssemblyDescriptor {
		i32 90624, ; uint32_t uncompressed_file_size
		i1 false, ; bool loaded
		i32 18591264; uint32_t buffer_offset
	}, ; 145: System.Runtime.Numerics
	%struct.CompressedAssemblyDescriptor {
		i32 7168, ; uint32_t uncompressed_file_size
		i1 false, ; bool loaded
		i32 18681888; uint32_t buffer_offset
	}, ; 146: System.Runtime.Serialization.Formatters
	%struct.CompressedAssemblyDescriptor {
		i32 17408, ; uint32_t uncompressed_file_size
		i1 false, ; bool loaded
		i32 18689056; uint32_t buffer_offset
	}, ; 147: System.Runtime
	%struct.CompressedAssemblyDescriptor {
		i32 124416, ; uint32_t uncompressed_file_size
		i1 false, ; bool loaded
		i32 18706464; uint32_t buffer_offset
	}, ; 148: System.Security.Cryptography
	%struct.CompressedAssemblyDescriptor {
		i32 31232, ; uint32_t uncompressed_file_size
		i1 false, ; bool loaded
		i32 18830880; uint32_t buffer_offset
	}, ; 149: System.Text.Encodings.Web
	%struct.CompressedAssemblyDescriptor {
		i32 384512, ; uint32_t uncompressed_file_size
		i1 false, ; bool loaded
		i32 18862112; uint32_t buffer_offset
	}, ; 150: System.Text.Json
	%struct.CompressedAssemblyDescriptor {
		i32 335872, ; uint32_t uncompressed_file_size
		i1 false, ; bool loaded
		i32 19246624; uint32_t buffer_offset
	}, ; 151: System.Text.RegularExpressions
	%struct.CompressedAssemblyDescriptor {
		i32 5120, ; uint32_t uncompressed_file_size
		i1 false, ; bool loaded
		i32 19582496; uint32_t buffer_offset
	}, ; 152: System.Threading.Thread
	%struct.CompressedAssemblyDescriptor {
		i32 12288, ; uint32_t uncompressed_file_size
		i1 false, ; bool loaded
		i32 19587616; uint32_t buffer_offset
	}, ; 153: System.Threading
	%struct.CompressedAssemblyDescriptor {
		i32 41984, ; uint32_t uncompressed_file_size
		i1 false, ; bool loaded
		i32 19599904; uint32_t buffer_offset
	}, ; 154: System.Transactions.Local
	%struct.CompressedAssemblyDescriptor {
		i32 5120, ; uint32_t uncompressed_file_size
		i1 false, ; bool loaded
		i32 19641888; uint32_t buffer_offset
	}, ; 155: System.Xml.ReaderWriter
	%struct.CompressedAssemblyDescriptor {
		i32 5120, ; uint32_t uncompressed_file_size
		i1 false, ; bool loaded
		i32 19647008; uint32_t buffer_offset
	}, ; 156: System
	%struct.CompressedAssemblyDescriptor {
		i32 6656, ; uint32_t uncompressed_file_size
		i1 false, ; bool loaded
		i32 19652128; uint32_t buffer_offset
	}, ; 157: netstandard
	%struct.CompressedAssemblyDescriptor {
		i32 2422272, ; uint32_t uncompressed_file_size
		i1 false, ; bool loaded
		i32 19658784; uint32_t buffer_offset
	}, ; 158: System.Private.CoreLib
	%struct.CompressedAssemblyDescriptor {
		i32 171008, ; uint32_t uncompressed_file_size
		i1 false, ; bool loaded
		i32 22081056; uint32_t buffer_offset
	}, ; 159: Java.Interop
	%struct.CompressedAssemblyDescriptor {
		i32 31744, ; uint32_t uncompressed_file_size
		i1 false, ; bool loaded
		i32 22252064; uint32_t buffer_offset
	}, ; 160: Mono.Android.Export
	%struct.CompressedAssemblyDescriptor {
		i32 22368, ; uint32_t uncompressed_file_size
		i1 false, ; bool loaded
		i32 22283808; uint32_t buffer_offset
	}, ; 161: Mono.Android.Runtime
	%struct.CompressedAssemblyDescriptor {
		i32 2225664, ; uint32_t uncompressed_file_size
		i1 false, ; bool loaded
		i32 22306176; uint32_t buffer_offset
	} ; 162: Mono.Android
], align 4

@uncompressed_assemblies_data_size = dso_local local_unnamed_addr constant i32 24531840, align 4

@uncompressed_assemblies_data_buffer = dso_local local_unnamed_addr global [24531840 x i8] zeroinitializer, align 1

; Metadata
!llvm.module.flags = !{!0, !1, !7, !8, !9, !10}
!0 = !{i32 1, !"wchar_size", i32 4}
!1 = !{i32 7, !"PIC Level", i32 2}
!llvm.ident = !{!2}
!2 = !{!".NET for Android remotes/origin/release/10.0.1xx @ d549e1dc4e2a083b08b4f24cb5495e81b99d79b5"}
!3 = !{!4, !4, i64 0}
!4 = !{!"any pointer", !5, i64 0}
!5 = !{!"omnipotent char", !6, i64 0}
!6 = !{!"Simple C++ TBAA"}
!7 = !{i32 1, !"branch-target-enforcement", i32 0}
!8 = !{i32 1, !"sign-return-address", i32 0}
!9 = !{i32 1, !"sign-return-address-all", i32 0}
!10 = !{i32 1, !"sign-return-address-with-bkey", i32 0}
