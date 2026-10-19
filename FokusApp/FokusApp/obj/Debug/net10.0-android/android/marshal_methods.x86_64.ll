; ModuleID = 'marshal_methods.x86_64.ll'
source_filename = "marshal_methods.x86_64.ll"
target datalayout = "e-m:e-p270:32:32-p271:32:32-p272:64:64-i64:64-f80:128-n8:16:32:64-S128"
target triple = "x86_64-unknown-linux-android21"

%struct.MarshalMethodName = type {
	i64, ; uint64_t id
	ptr ; char* name
}

%struct.MarshalMethodsManagedClass = type {
	i32, ; uint32_t token
	ptr ; MonoClass klass
}

@assembly_image_cache = dso_local local_unnamed_addr global [365 x ptr] zeroinitializer, align 16

; Each entry maps hash of an assembly name to an index into the `assembly_image_cache` array
@assembly_image_cache_hashes = dso_local local_unnamed_addr constant [1095 x i64] [
	i64 u0x001e58127c546039, ; 0: lib_System.Globalization.dll.so => 43
	i64 u0x0024d0f62dee05bd, ; 1: Xamarin.KotlinX.Coroutines.Core.dll => 319
	i64 u0x0071cf2d27b7d61e, ; 2: lib_Xamarin.AndroidX.SwipeRefreshLayout.dll.so => 298
	i64 u0x01109b0e4d99e61f, ; 3: System.ComponentModel.Annotations.dll => 14
	i64 u0x02123411c4e01926, ; 4: lib_Xamarin.AndroidX.Navigation.Runtime.dll.so => 286
	i64 u0x022f31be406de945, ; 5: Microsoft.Extensions.Options.ConfigurationExtensions => 217
	i64 u0x0284512fad379f7e, ; 6: System.Runtime.Handles => 108
	i64 u0x02a4c5a44384f885, ; 7: Microsoft.Extensions.Caching.Memory => 194
	i64 u0x02abedc11addc1ed, ; 8: lib_Mono.Android.Runtime.dll.so => 175
	i64 u0x02f55bf70672f5c8, ; 9: lib_System.IO.FileSystem.DriveInfo.dll.so => 49
	i64 u0x032267b2a94db371, ; 10: lib_Xamarin.AndroidX.AppCompat.dll.so => 240
	i64 u0x03621c804933a890, ; 11: System.Buffers => 8
	i64 u0x0363ac97a4cb84e6, ; 12: SQLitePCLRaw.provider.e_sqlite3.dll => 230
	i64 u0x0399610510a38a38, ; 13: lib_System.Private.DataContractSerialization.dll.so => 89
	i64 u0x043032f1d071fae0, ; 14: ru/Microsoft.Maui.Controls.resources => 347
	i64 u0x044440a55165631e, ; 15: lib-cs-Microsoft.Maui.Controls.resources.dll.so => 325
	i64 u0x046eb1581a80c6b0, ; 16: vi/Microsoft.Maui.Controls.resources => 353
	i64 u0x047408741db2431a, ; 17: Xamarin.AndroidX.DynamicAnimation => 260
	i64 u0x0517ef04e06e9f76, ; 18: System.Net.Primitives => 73
	i64 u0x0565d18c6da3de38, ; 19: Xamarin.AndroidX.RecyclerView => 290
	i64 u0x057bf9fa9fb09f7c, ; 20: Microsoft.Data.Sqlite.dll => 188
	i64 u0x0581db89237110e9, ; 21: lib_System.Collections.dll.so => 13
	i64 u0x05989cb940b225a9, ; 22: Microsoft.Maui.dll => 223
	i64 u0x05a1c25e78e22d87, ; 23: lib_System.Runtime.CompilerServices.Unsafe.dll.so => 105
	i64 u0x05ef98b6a1db882c, ; 24: lib_Microsoft.Data.Sqlite.dll.so => 188
	i64 u0x06076b5d2b581f08, ; 25: zh-HK/Microsoft.Maui.Controls.resources => 354
	i64 u0x06388ffe9f6c161a, ; 26: System.Xml.Linq.dll => 160
	i64 u0x06600c4c124cb358, ; 27: System.Configuration.dll => 20
	i64 u0x067f95c5ddab55b3, ; 28: lib_Xamarin.AndroidX.Fragment.Ktx.dll.so => 265
	i64 u0x0680a433c781bb3d, ; 29: Xamarin.AndroidX.Collection.Jvm => 247
	i64 u0x0690533f9fc14683, ; 30: lib_Microsoft.AspNetCore.Components.dll.so => 182
	i64 u0x069fff96ec92a91d, ; 31: System.Xml.XPath.dll => 165
	i64 u0x070b0847e18dab68, ; 32: Xamarin.AndroidX.Emoji2.ViewsHelper.dll => 262
	i64 u0x0739448d84d3b016, ; 33: lib_Xamarin.AndroidX.VectorDrawable.dll.so => 302
	i64 u0x07469f2eecce9e85, ; 34: mscorlib.dll => 171
	i64 u0x07c57877c7ba78ad, ; 35: ru/Microsoft.Maui.Controls.resources.dll => 347
	i64 u0x07dcdc7460a0c5e4, ; 36: System.Collections.NonGeneric => 11
	i64 u0x08122e52765333c8, ; 37: lib_Microsoft.Extensions.Logging.Debug.dll.so => 215
	i64 u0x088610fc2509f69e, ; 38: lib_Xamarin.AndroidX.VectorDrawable.Animated.dll.so => 303
	i64 u0x08a7c865576bbde7, ; 39: System.Reflection.Primitives => 99
	i64 u0x08c9d051a4a817e5, ; 40: Xamarin.AndroidX.CustomView.PoolingContainer.dll => 258
	i64 u0x08da3c7dbc340741, ; 41: en/FokusApp.resources.dll => 0
	i64 u0x08f3c9788ee2153c, ; 42: Xamarin.AndroidX.DrawerLayout => 259
	i64 u0x09138715c92dba90, ; 43: lib_System.ComponentModel.Annotations.dll.so => 14
	i64 u0x0919c28b89381a0b, ; 44: lib_Microsoft.Extensions.Options.dll.so => 216
	i64 u0x092266563089ae3e, ; 45: lib_System.Collections.NonGeneric.dll.so => 11
	i64 u0x09d144a7e214d457, ; 46: System.Security.Cryptography => 130
	i64 u0x09e2b9f743db21a8, ; 47: lib_System.Reflection.Metadata.dll.so => 98
	i64 u0x0a805f95d98f597b, ; 48: lib_Microsoft.Extensions.Caching.Abstractions.dll.so => 193
	i64 u0x0abb3e2b271edc45, ; 49: System.Threading.Channels.dll => 144
	i64 u0x0b06b1feab070143, ; 50: System.Formats.Tar => 40
	i64 u0x0b3b632c3bbee20c, ; 51: sk/Microsoft.Maui.Controls.resources => 348
	i64 u0x0b6aff547b84fbe9, ; 52: Xamarin.KotlinX.Serialization.Core.Jvm => 322
	i64 u0x0be2e1f8ce4064ed, ; 53: Xamarin.AndroidX.ViewPager => 305
	i64 u0x0c3ca6cc978e2aae, ; 54: pt-BR/Microsoft.Maui.Controls.resources => 344
	i64 u0x0c59ad9fbbd43abe, ; 55: Mono.Android => 176
	i64 u0x0c65741e86371ee3, ; 56: lib_Xamarin.Android.Glide.GifDecoder.dll.so => 234
	i64 u0x0c74af560004e816, ; 57: Microsoft.Win32.Registry.dll => 6
	i64 u0x0c7790f60165fc06, ; 58: lib_Microsoft.Maui.Essentials.dll.so => 224
	i64 u0x0c83c82812e96127, ; 59: lib_System.Net.Mail.dll.so => 69
	i64 u0x0cce4bce83380b7f, ; 60: Xamarin.AndroidX.Security.SecurityCrypto => 295
	i64 u0x0d13cd7cce4284e4, ; 61: System.Security.SecureString => 133
	i64 u0x0d63f4f73521c24f, ; 62: lib_Xamarin.AndroidX.SavedState.SavedState.Ktx.dll.so => 294
	i64 u0x0e04e702012f8463, ; 63: Xamarin.AndroidX.Emoji2 => 261
	i64 u0x0e14e73a54dda68e, ; 64: lib_System.Net.NameResolution.dll.so => 70
	i64 u0x0f37dd7a62ae99af, ; 65: lib_Xamarin.AndroidX.Collection.Ktx.dll.so => 248
	i64 u0x0f5e7abaa7cf470a, ; 66: System.Net.HttpListener => 68
	i64 u0x1001f97bbe242e64, ; 67: System.IO.UnmanagedMemoryStream => 58
	i64 u0x102a31b45304b1da, ; 68: Xamarin.AndroidX.CustomView => 257
	i64 u0x1065c4cb554c3d75, ; 69: System.IO.IsolatedStorage.dll => 53
	i64 u0x10a6fa3e9d5ee60e, ; 70: FokusApp.Migrations.dll => 358
	i64 u0x10f6cfcbcf801616, ; 71: System.IO.Compression.Brotli => 44
	i64 u0x114443cdcf2091f1, ; 72: System.Security.Cryptography.Primitives => 128
	i64 u0x11a603952763e1d4, ; 73: System.Net.Mail => 69
	i64 u0x11a70d0e1009fb11, ; 74: System.Net.WebSockets.dll => 84
	i64 u0x11f26371eee0d3c1, ; 75: lib_Xamarin.AndroidX.Lifecycle.Runtime.Ktx.dll.so => 275
	i64 u0x11fbe62d469cc1c8, ; 76: Microsoft.VisualStudio.DesignTools.TapContract.dll => 362
	i64 u0x12128b3f59302d47, ; 77: lib_System.Xml.Serialization.dll.so => 162
	i64 u0x123639456fb056da, ; 78: System.Reflection.Emit.Lightweight.dll => 95
	i64 u0x12521e9764603eaa, ; 79: lib_System.Resources.Reader.dll.so => 102
	i64 u0x125b7f94acb989db, ; 80: Xamarin.AndroidX.RecyclerView.dll => 290
	i64 u0x12d3b63863d4ab0b, ; 81: lib_System.Threading.Overlapped.dll.so => 145
	i64 u0x134eab1061c395ee, ; 82: System.Transactions => 155
	i64 u0x138567fa954faa55, ; 83: Xamarin.AndroidX.Browser => 244
	i64 u0x13a01de0cbc3f06c, ; 84: lib-fr-Microsoft.Maui.Controls.resources.dll.so => 331
	i64 u0x13beedefb0e28a45, ; 85: lib_System.Xml.XmlDocument.dll.so => 166
	i64 u0x13f1e5e209e91af4, ; 86: lib_Java.Interop.dll.so => 173
	i64 u0x13f1e880c25d96d1, ; 87: he/Microsoft.Maui.Controls.resources => 332
	i64 u0x143d8ea60a6a4011, ; 88: Microsoft.Extensions.DependencyInjection.Abstractions => 201
	i64 u0x1497051b917530bd, ; 89: lib_System.Net.WebSockets.dll.so => 84
	i64 u0x14b78ce3adce0011, ; 90: Microsoft.VisualStudio.DesignTools.TapContract => 362
	i64 u0x14d612a531c79c05, ; 91: Xamarin.JSpecify.dll => 316
	i64 u0x14e68447938213b7, ; 92: Xamarin.AndroidX.Collection.Ktx.dll => 248
	i64 u0x152a448bd1e745a7, ; 93: Microsoft.Win32.Primitives => 5
	i64 u0x1557de0138c445f4, ; 94: lib_Microsoft.Win32.Registry.dll.so => 6
	i64 u0x15bdc156ed462f2f, ; 95: lib_System.IO.FileSystem.dll.so => 52
	i64 u0x15e300c2c1668655, ; 96: System.Resources.Writer.dll => 104
	i64 u0x16054fdcb6b3098b, ; 97: Microsoft.Extensions.DependencyModel.dll => 202
	i64 u0x16bf2a22df043a09, ; 98: System.IO.Pipes.dll => 57
	i64 u0x16ea2b318ad2d830, ; 99: System.Security.Cryptography.Algorithms => 123
	i64 u0x16eeae54c7ebcc08, ; 100: System.Reflection.dll => 101
	i64 u0x17125c9a85b4929f, ; 101: lib_netstandard.dll.so => 172
	i64 u0x1716866f7416792e, ; 102: lib_System.Security.AccessControl.dll.so => 121
	i64 u0x174f71c46216e44a, ; 103: Xamarin.KotlinX.Coroutines.Core => 319
	i64 u0x1752c12f1e1fc00c, ; 104: System.Core => 22
	i64 u0x17b56e25558a5d36, ; 105: lib-hu-Microsoft.Maui.Controls.resources.dll.so => 335
	i64 u0x17f9358913beb16a, ; 106: System.Text.Encodings.Web => 140
	i64 u0x1809fb23f29ba44a, ; 107: lib_System.Reflection.TypeExtensions.dll.so => 100
	i64 u0x18402a709e357f3b, ; 108: lib_Xamarin.KotlinX.Serialization.Core.Jvm.dll.so => 322
	i64 u0x18a9befae51bb361, ; 109: System.Net.WebClient => 80
	i64 u0x18f0ce884e87d89a, ; 110: nb/Microsoft.Maui.Controls.resources.dll => 341
	i64 u0x19777fba3c41b398, ; 111: Xamarin.AndroidX.Startup.StartupRuntime.dll => 297
	i64 u0x19a4c090f14ebb66, ; 112: System.Security.Claims => 122
	i64 u0x1a91866a319e9259, ; 113: lib_System.Collections.Concurrent.dll.so => 9
	i64 u0x1aac34d1917ba5d3, ; 114: lib_System.dll.so => 169
	i64 u0x1aad60783ffa3e5b, ; 115: lib-th-Microsoft.Maui.Controls.resources.dll.so => 350
	i64 u0x1aea8f1c3b282172, ; 116: lib_System.Net.Ping.dll.so => 72
	i64 u0x1b4b7a1d0d265fa2, ; 117: Xamarin.Android.Glide.DiskLruCache => 233
	i64 u0x1b8700ce6e547c0b, ; 118: lib_Microsoft.AspNetCore.Components.Forms.dll.so => 183
	i64 u0x1bbdb16cfa73e785, ; 119: Xamarin.AndroidX.Lifecycle.Runtime.Ktx.Android => 276
	i64 u0x1bc766e07b2b4241, ; 120: Xamarin.AndroidX.ResourceInspection.Annotation.dll => 291
	i64 u0x1c292b1598348d77, ; 121: Microsoft.Extensions.Diagnostics.dll => 203
	i64 u0x1c5217a9e4973753, ; 122: lib_Microsoft.Extensions.FileProviders.Physical.dll.so => 208
	i64 u0x1c753b5ff15bce1b, ; 123: Mono.Android.Runtime.dll => 175
	i64 u0x1cd47467799d8250, ; 124: System.Threading.Tasks.dll => 149
	i64 u0x1d23eafdc6dc346c, ; 125: System.Globalization.Calendars.dll => 41
	i64 u0x1da4110562816681, ; 126: Xamarin.AndroidX.Security.SecurityCrypto.dll => 295
	i64 u0x1db6820994506bf5, ; 127: System.IO.FileSystem.AccessControl.dll => 48
	i64 u0x1dbb0c2c6a999acb, ; 128: System.Diagnostics.StackTrace => 31
	i64 u0x1e3d87657e9659bc, ; 129: Xamarin.AndroidX.Navigation.UI => 288
	i64 u0x1e71143913d56c10, ; 130: lib-ko-Microsoft.Maui.Controls.resources.dll.so => 339
	i64 u0x1e7c31185e2fb266, ; 131: lib_System.Threading.Tasks.Parallel.dll.so => 148
	i64 u0x1ed8fcce5e9b50a0, ; 132: Microsoft.Extensions.Options.dll => 216
	i64 u0x1f055d15d807e1b2, ; 133: System.Xml.XmlSerializer => 167
	i64 u0x1f1ed22c1085f044, ; 134: lib_System.Diagnostics.FileVersionInfo.dll.so => 29
	i64 u0x1f61df9c5b94d2c1, ; 135: lib_System.Numerics.dll.so => 87
	i64 u0x1f750bb5421397de, ; 136: lib_Xamarin.AndroidX.Tracing.Tracing.dll.so => 299
	i64 u0x20237ea48006d7a8, ; 137: lib_System.Net.WebClient.dll.so => 80
	i64 u0x209375905fcc1bad, ; 138: lib_System.IO.Compression.Brotli.dll.so => 44
	i64 u0x20fab3cf2dfbc8df, ; 139: lib_System.Diagnostics.Process.dll.so => 30
	i64 u0x210c5bb16b7260a2, ; 140: lib_Microsoft.Extensions.Validation.dll.so => 219
	i64 u0x2110167c128cba15, ; 141: System.Globalization => 43
	i64 u0x21419508838f7547, ; 142: System.Runtime.CompilerServices.VisualC => 106
	i64 u0x2174319c0d835bc9, ; 143: System.Runtime => 120
	i64 u0x2198e5bc8b7153fa, ; 144: Xamarin.AndroidX.Annotation.Experimental.dll => 238
	i64 u0x219ea1b751a4dee4, ; 145: lib_System.IO.Compression.ZipFile.dll.so => 46
	i64 u0x21cc7e445dcd5469, ; 146: System.Reflection.Emit.ILGeneration => 94
	i64 u0x220fd4f2e7c48170, ; 147: th/Microsoft.Maui.Controls.resources => 350
	i64 u0x224538d85ed15a82, ; 148: System.IO.Pipes => 57
	i64 u0x22908438c6bed1af, ; 149: lib_System.Threading.Timer.dll.so => 152
	i64 u0x22fbc14e981e3b45, ; 150: lib_Microsoft.VisualStudio.DesignTools.MobileTapContracts.dll.so => 361
	i64 u0x236b0c00e2f0397d, ; 151: lib_FokusApp.Migrations.dll.so => 358
	i64 u0x237be844f1f812c7, ; 152: System.Threading.Thread.dll => 150
	i64 u0x23807c59646ec4f3, ; 153: lib_Microsoft.EntityFrameworkCore.dll.so => 189
	i64 u0x23852b3bdc9f7096, ; 154: System.Resources.ResourceManager => 103
	i64 u0x23986dd7e5d4fc01, ; 155: System.IO.FileSystem.Primitives.dll => 50
	i64 u0x2407aef2bbe8fadf, ; 156: System.Console => 21
	i64 u0x240abe014b27e7d3, ; 157: Xamarin.AndroidX.Core.dll => 253
	i64 u0x247619fe4413f8bf, ; 158: System.Runtime.Serialization.Primitives.dll => 117
	i64 u0x24de8d301281575e, ; 159: Xamarin.Android.Glide => 231
	i64 u0x252073cc3caa62c2, ; 160: fr/Microsoft.Maui.Controls.resources.dll => 331
	i64 u0x256b8d41255f01b1, ; 161: Xamarin.Google.Crypto.Tink.Android => 312
	i64 u0x25a0a7eff76ea08e, ; 162: SQLitePCLRaw.batteries_v2.dll => 227
	i64 u0x2662c629b96b0b30, ; 163: lib_Xamarin.Kotlin.StdLib.dll.so => 317
	i64 u0x268c1439f13bcc29, ; 164: lib_Microsoft.Extensions.Primitives.dll.so => 218
	i64 u0x26a670e154a9c54b, ; 165: System.Reflection.Extensions.dll => 97
	i64 u0x26d077d9678fe34f, ; 166: System.IO.dll => 59
	i64 u0x273f3515de5faf0d, ; 167: id/Microsoft.Maui.Controls.resources.dll => 336
	i64 u0x2742545f9094896d, ; 168: hr/Microsoft.Maui.Controls.resources => 334
	i64 u0x27458c1163a5ce22, ; 169: Microsoft.Extensions.Validation => 219
	i64 u0x274d85d83ad40513, ; 170: lib_Xamarin.AndroidX.Window.WindowCore.dll.so => 308
	i64 u0x2759af78ab94d39b, ; 171: System.Net.WebSockets => 84
	i64 u0x27b2b16f3e9de038, ; 172: Xamarin.Google.Crypto.Tink.Android.dll => 312
	i64 u0x27b410442fad6cf1, ; 173: Java.Interop.dll => 173
	i64 u0x27b97e0d52c3034a, ; 174: System.Diagnostics.Debug => 27
	i64 u0x2801845a2c71fbfb, ; 175: System.Net.Primitives.dll => 73
	i64 u0x286835e259162700, ; 176: lib_Xamarin.AndroidX.ProfileInstaller.ProfileInstaller.dll.so => 289
	i64 u0x28e52865585a1ebe, ; 177: Microsoft.Extensions.Diagnostics.Abstractions.dll => 204
	i64 u0x2949f3617a02c6b2, ; 178: Xamarin.AndroidX.ExifInterface => 263
	i64 u0x29aeab763a527e52, ; 179: lib_Xamarin.AndroidX.Navigation.Common.Android.dll.so => 284
	i64 u0x29f947844fb7fc11, ; 180: Microsoft.Maui.Controls.HotReload.Forms => 360
	i64 u0x2a128783efe70ba0, ; 181: uk/Microsoft.Maui.Controls.resources.dll => 352
	i64 u0x2a3b095612184159, ; 182: lib_System.Net.NetworkInformation.dll.so => 71
	i64 u0x2a6507a5ffabdf28, ; 183: System.Diagnostics.TraceSource.dll => 34
	i64 u0x2ad156c8e1354139, ; 184: fi/Microsoft.Maui.Controls.resources => 330
	i64 u0x2ad5d6b13b7a3e04, ; 185: System.ComponentModel.DataAnnotations.dll => 15
	i64 u0x2af298f63581d886, ; 186: System.Text.RegularExpressions.dll => 142
	i64 u0x2afc1c4f898552ee, ; 187: lib_System.Formats.Asn1.dll.so => 39
	i64 u0x2b148910ed40fbf9, ; 188: zh-Hant/Microsoft.Maui.Controls.resources.dll => 356
	i64 u0x2b4d4904cebfa4e9, ; 189: Microsoft.Extensions.FileSystemGlobbing => 209
	i64 u0x2b6989d78cba9a15, ; 190: Xamarin.AndroidX.Concurrent.Futures.dll => 249
	i64 u0x2c8bd14bb93a7d82, ; 191: lib-pl-Microsoft.Maui.Controls.resources.dll.so => 343
	i64 u0x2cbd9262ca785540, ; 192: lib_System.Text.Encoding.CodePages.dll.so => 137
	i64 u0x2cc9e1fed6257257, ; 193: lib_System.Reflection.Emit.Lightweight.dll.so => 95
	i64 u0x2cd723e9fe623c7c, ; 194: lib_System.Private.Xml.Linq.dll.so => 91
	i64 u0x2d169d318a968379, ; 195: System.Threading.dll => 153
	i64 u0x2d20145f27cfc1d2, ; 196: Xamarin.AndroidX.Window.WindowCore.Jvm.dll => 309
	i64 u0x2d47774b7d993f59, ; 197: sv/Microsoft.Maui.Controls.resources.dll => 349
	i64 u0x2d5ffcae1ad0aaca, ; 198: System.Data.dll => 25
	i64 u0x2db915caf23548d2, ; 199: System.Text.Json.dll => 141
	i64 u0x2dcaa0bb15a4117a, ; 200: System.IO.UnmanagedMemoryStream.dll => 58
	i64 u0x2e2ced2c3c6a1edc, ; 201: lib_System.Threading.AccessControl.dll.so => 143
	i64 u0x2e5a40c319acb800, ; 202: System.IO.FileSystem => 52
	i64 u0x2e6f1f226821322a, ; 203: el/Microsoft.Maui.Controls.resources.dll => 328
	i64 u0x2e8ff3fae87a8245, ; 204: lib_Microsoft.JSInterop.dll.so => 220
	i64 u0x2f02f94df3200fe5, ; 205: System.Diagnostics.Process => 30
	i64 u0x2f2e98e1c89b1aff, ; 206: System.Xml.ReaderWriter => 161
	i64 u0x2f5911d9ba814e4e, ; 207: System.Diagnostics.Tracing => 35
	i64 u0x2f84070a459bc31f, ; 208: lib_System.Xml.dll.so => 168
	i64 u0x2feb4d2fcda05cfd, ; 209: Microsoft.Extensions.Caching.Abstractions.dll => 193
	i64 u0x309ee9eeec09a71e, ; 210: lib_Xamarin.AndroidX.Fragment.dll.so => 264
	i64 u0x30c6dda129408828, ; 211: System.IO.IsolatedStorage => 53
	i64 u0x310d9651ec86c411, ; 212: Microsoft.Extensions.FileProviders.Embedded => 207
	i64 u0x31195fef5d8fb552, ; 213: _Microsoft.Android.Resource.Designer.dll => 364
	i64 u0x312c8ed623cbfc8d, ; 214: Xamarin.AndroidX.Window.dll => 307
	i64 u0x31496b779ed0663d, ; 215: lib_System.Reflection.DispatchProxy.dll.so => 93
	i64 u0x32243413e774362a, ; 216: Xamarin.AndroidX.CardView.dll => 245
	i64 u0x3235427f8d12dae1, ; 217: lib_System.Drawing.Primitives.dll.so => 36
	i64 u0x329753a17a517811, ; 218: fr/Microsoft.Maui.Controls.resources => 331
	i64 u0x32aa989ff07a84ff, ; 219: lib_System.Xml.ReaderWriter.dll.so => 161
	i64 u0x33642d5508314e46, ; 220: Microsoft.Extensions.FileSystemGlobbing.dll => 209
	i64 u0x33829542f112d59b, ; 221: System.Collections.Immutable => 10
	i64 u0x33a31443733849fe, ; 222: lib-es-Microsoft.Maui.Controls.resources.dll.so => 329
	i64 u0x341abc357fbb4ebf, ; 223: lib_System.Net.Sockets.dll.so => 79
	i64 u0x346a212343615ac5, ; 224: lib_System.Linq.AsyncEnumerable.dll.so => 60
	i64 u0x3496c1e2dcaf5ecc, ; 225: lib_System.IO.Pipes.AccessControl.dll.so => 56
	i64 u0x34bd01fd4be06ee3, ; 226: lib_Microsoft.Extensions.FileProviders.Composite.dll.so => 206
	i64 u0x34dfd74fe2afcf37, ; 227: Microsoft.Maui => 223
	i64 u0x34e292762d9615df, ; 228: cs/Microsoft.Maui.Controls.resources.dll => 325
	i64 u0x3508234247f48404, ; 229: Microsoft.Maui.Controls => 221
	i64 u0x353590da528c9d22, ; 230: System.ComponentModel.Annotations => 14
	i64 u0x3549870798b4cd30, ; 231: lib_Xamarin.AndroidX.ViewPager2.dll.so => 306
	i64 u0x355282fc1c909694, ; 232: Microsoft.Extensions.Configuration => 195
	i64 u0x3552fc5d578f0fbf, ; 233: Xamarin.AndroidX.Arch.Core.Common => 242
	i64 u0x355c649948d55d97, ; 234: lib_System.Runtime.Intrinsics.dll.so => 112
	i64 u0x35ea9d1c6834bc8c, ; 235: Xamarin.AndroidX.Lifecycle.ViewModel.Ktx.dll => 279
	i64 u0x3628ab68db23a01a, ; 236: lib_System.Diagnostics.Tools.dll.so => 33
	i64 u0x3673b042508f5b6b, ; 237: lib_System.Runtime.Extensions.dll.so => 107
	i64 u0x36740f1a8ecdc6c4, ; 238: System.Numerics => 87
	i64 u0x36b2b50fdf589ae2, ; 239: System.Reflection.Emit.Lightweight => 95
	i64 u0x36cada77dc79928b, ; 240: System.IO.MemoryMappedFiles => 54
	i64 u0x374ef46b06791af6, ; 241: System.Reflection.Primitives.dll => 99
	i64 u0x376bf93e521a5417, ; 242: lib_Xamarin.Jetbrains.Annotations.dll.so => 315
	i64 u0x37bc29f3183003b6, ; 243: lib_System.IO.dll.so => 59
	i64 u0x380134e03b1e160a, ; 244: System.Collections.Immutable.dll => 10
	i64 u0x38049b5c59b39324, ; 245: System.Runtime.CompilerServices.Unsafe => 105
	i64 u0x385c17636bb6fe6e, ; 246: Xamarin.AndroidX.CustomView.dll => 257
	i64 u0x38869c811d74050e, ; 247: System.Net.NameResolution.dll => 70
	i64 u0x393c226616977fdb, ; 248: lib_Xamarin.AndroidX.ViewPager.dll.so => 305
	i64 u0x395e37c3334cf82a, ; 249: lib-ca-Microsoft.Maui.Controls.resources.dll.so => 324
	i64 u0x39c3107c28752af1, ; 250: lib_Microsoft.Extensions.FileProviders.Abstractions.dll.so => 205
	i64 u0x3ab5859054645f72, ; 251: System.Security.Cryptography.Primitives.dll => 128
	i64 u0x3ad75090c3fac0e9, ; 252: lib_Xamarin.AndroidX.ResourceInspection.Annotation.dll.so => 291
	i64 u0x3ae44ac43a1fbdbb, ; 253: System.Runtime.Serialization => 119
	i64 u0x3b860f9932505633, ; 254: lib_System.Text.Encoding.Extensions.dll.so => 138
	i64 u0x3be6248c2bc7dc8c, ; 255: Microsoft.JSInterop.dll => 220
	i64 u0x3be99b43dd39dd37, ; 256: Xamarin.AndroidX.SavedState.SavedState.Android => 293
	i64 u0x3c3aafb6b3a00bf6, ; 257: lib_System.Security.Cryptography.X509Certificates.dll.so => 129
	i64 u0x3c4049146b59aa90, ; 258: System.Runtime.InteropServices.JavaScript => 109
	i64 u0x3c7c495f58ac5ee9, ; 259: Xamarin.Kotlin.StdLib => 317
	i64 u0x3c7e5ed3d5db71bb, ; 260: System.Security => 134
	i64 u0x3cd9d281d402eb9b, ; 261: Xamarin.AndroidX.Browser.dll => 244
	i64 u0x3cdce1f9610469be, ; 262: FokusApp.Migrations => 358
	i64 u0x3d1c50cc001a991e, ; 263: Xamarin.Google.Guava.ListenableFuture.dll => 314
	i64 u0x3d2b1913edfc08d7, ; 264: lib_System.Threading.ThreadPool.dll.so => 151
	i64 u0x3d46f0b995082740, ; 265: System.Xml.Linq => 160
	i64 u0x3d8a8f400514a790, ; 266: Xamarin.AndroidX.Fragment.Ktx.dll => 265
	i64 u0x3d9c2a242b040a50, ; 267: lib_Xamarin.AndroidX.Core.dll.so => 253
	i64 u0x3da7781d6333a8fe, ; 268: SQLitePCLRaw.batteries_v2 => 227
	i64 u0x3db495de2204755c, ; 269: Microsoft.Extensions.Configuration.FileExtensions => 198
	i64 u0x3dbb6b9f5ab90fa7, ; 270: lib_Xamarin.AndroidX.DynamicAnimation.dll.so => 260
	i64 u0x3e5441657549b213, ; 271: Xamarin.AndroidX.ResourceInspection.Annotation => 291
	i64 u0x3e57d4d195c53c2e, ; 272: System.Reflection.TypeExtensions => 100
	i64 u0x3e616ab4ed1f3f15, ; 273: lib_System.Data.dll.so => 25
	i64 u0x3e7f8912b96e5065, ; 274: Microsoft.AspNetCore.Components.WebView.dll => 185
	i64 u0x3f1d226e6e06db7e, ; 275: Xamarin.AndroidX.SlidingPaneLayout.dll => 296
	i64 u0x3f510adf788828dd, ; 276: System.Threading.Tasks.Extensions => 147
	i64 u0x3f6f5914291cdcf7, ; 277: Microsoft.Extensions.Hosting.Abstractions => 210
	i64 u0x407a10bb4bf95829, ; 278: lib_Xamarin.AndroidX.Navigation.Common.dll.so => 283
	i64 u0x40c98b6bd77346d4, ; 279: Microsoft.VisualBasic.dll => 4
	i64 u0x41833cf766d27d96, ; 280: mscorlib => 171
	i64 u0x41cab042be111c34, ; 281: lib_Xamarin.AndroidX.AppCompat.AppCompatResources.dll.so => 241
	i64 u0x41f93add55d80a27, ; 282: lib_Microsoft.Extensions.Localization.dll.so => 211
	i64 u0x423a9ecc4d905a88, ; 283: lib_System.Resources.ResourceManager.dll.so => 103
	i64 u0x423bf51ae7def810, ; 284: System.Xml.XPath => 165
	i64 u0x42462ff15ddba223, ; 285: System.Resources.Reader.dll => 102
	i64 u0x4291015ff4e5ef71, ; 286: Xamarin.AndroidX.Core.ViewTree.dll => 255
	i64 u0x42a31b86e6ccc3f0, ; 287: System.Diagnostics.Contracts => 26
	i64 u0x430e95b891249788, ; 288: lib_System.Reflection.Emit.dll.so => 96
	i64 u0x43375950ec7c1b6a, ; 289: netstandard.dll => 172
	i64 u0x434c4e1d9284cdae, ; 290: Mono.Android.dll => 176
	i64 u0x43505013578652a0, ; 291: lib_Xamarin.AndroidX.Activity.Ktx.dll.so => 236
	i64 u0x437d06c381ed575a, ; 292: lib_Microsoft.VisualBasic.dll.so => 4
	i64 u0x43950f84de7cc79a, ; 293: pl/Microsoft.Maui.Controls.resources.dll => 343
	i64 u0x439cd2a34ac0cca1, ; 294: FokusApp.dll => 1
	i64 u0x43c077442b230f64, ; 295: Xamarin.AndroidX.Tracing.Tracing.Android => 300
	i64 u0x43e8ca5bc927ff37, ; 296: lib_Xamarin.AndroidX.Emoji2.ViewsHelper.dll.so => 262
	i64 u0x448bd33429269b19, ; 297: Microsoft.CSharp => 2
	i64 u0x4499fa3c8e494654, ; 298: lib_System.Runtime.Serialization.Primitives.dll.so => 117
	i64 u0x4515080865a951a5, ; 299: Xamarin.Kotlin.StdLib.dll => 317
	i64 u0x453c1277f85cf368, ; 300: lib_Microsoft.EntityFrameworkCore.Abstractions.dll.so => 190
	i64 u0x4545802489b736b9, ; 301: Xamarin.AndroidX.Fragment.Ktx => 265
	i64 u0x454b4d1e66bb783c, ; 302: Xamarin.AndroidX.Lifecycle.Process => 272
	i64 u0x45c40276a42e283e, ; 303: System.Diagnostics.TraceSource => 34
	i64 u0x45d443f2a29adc37, ; 304: System.AppContext.dll => 7
	i64 u0x45fcc9fd66f25095, ; 305: Microsoft.Extensions.DependencyModel => 202
	i64 u0x46a4213bc97fe5ae, ; 306: lib-ru-Microsoft.Maui.Controls.resources.dll.so => 347
	i64 u0x47358bd471172e1d, ; 307: lib_System.Xml.Linq.dll.so => 160
	i64 u0x47daf4e1afbada10, ; 308: pt/Microsoft.Maui.Controls.resources => 345
	i64 u0x480c0a47dd42dd81, ; 309: lib_System.IO.MemoryMappedFiles.dll.so => 54
	i64 u0x49e952f19a4e2022, ; 310: System.ObjectModel => 88
	i64 u0x49f61f655a6a21de, ; 311: Microsoft.Extensions.Localization.Abstractions.dll => 212
	i64 u0x49f9e6948a8131e4, ; 312: lib_Xamarin.AndroidX.VersionedParcelable.dll.so => 304
	i64 u0x4a5667b2462a664b, ; 313: lib_Xamarin.AndroidX.Navigation.UI.dll.so => 288
	i64 u0x4a7a18981dbd56bc, ; 314: System.IO.Compression.FileSystem.dll => 45
	i64 u0x4aa5c60350917c06, ; 315: lib_Xamarin.AndroidX.Lifecycle.LiveData.Core.Ktx.dll.so => 271
	i64 u0x4b07a0ed0ab33ff4, ; 316: System.Runtime.Extensions.dll => 107
	i64 u0x4b576d47ac054f3c, ; 317: System.IO.FileSystem.AccessControl => 48
	i64 u0x4b7b6532ded934b7, ; 318: System.Text.Json => 141
	i64 u0x4c2029a97af23a8d, ; 319: Xamarin.AndroidX.Lifecycle.ViewModelSavedState.Android => 281
	i64 u0x4c7755cf07ad2d5f, ; 320: System.Net.Http.Json.dll => 66
	i64 u0x4ca014ceac582c86, ; 321: Microsoft.EntityFrameworkCore.Relational.dll => 191
	i64 u0x4cc5f15266470798, ; 322: lib_Xamarin.AndroidX.Loader.dll.so => 282
	i64 u0x4cf6f67dc77aacd2, ; 323: System.Net.NetworkInformation.dll => 71
	i64 u0x4d3183dd245425d4, ; 324: System.Net.WebSockets.Client.dll => 83
	i64 u0x4d479f968a05e504, ; 325: System.Linq.Expressions.dll => 61
	i64 u0x4d55a010ffc4faff, ; 326: System.Private.Xml => 92
	i64 u0x4d5cbe77561c5b2e, ; 327: System.Web.dll => 158
	i64 u0x4d77512dbd86ee4c, ; 328: lib_Xamarin.AndroidX.Arch.Core.Common.dll.so => 242
	i64 u0x4d7793536e79c309, ; 329: System.ServiceProcess => 136
	i64 u0x4d95fccc1f67c7ca, ; 330: System.Runtime.Loader.dll => 113
	i64 u0x4db014bf0ff1c9c1, ; 331: System.Linq.AsyncEnumerable => 60
	i64 u0x4dcf44c3c9b076a2, ; 332: it/Microsoft.Maui.Controls.resources.dll => 337
	i64 u0x4dd9247f1d2c3235, ; 333: Xamarin.AndroidX.Loader.dll => 282
	i64 u0x4df510084e2a0bae, ; 334: Microsoft.JSInterop => 220
	i64 u0x4e2aeee78e2c4a87, ; 335: Xamarin.AndroidX.ProfileInstaller.ProfileInstaller => 289
	i64 u0x4e32f00cb0937401, ; 336: Mono.Android.Runtime => 175
	i64 u0x4e5eea4668ac2b18, ; 337: System.Text.Encoding.CodePages => 137
	i64 u0x4ebd0c4b82c5eefc, ; 338: lib_System.Threading.Channels.dll.so => 144
	i64 u0x4ee8eaa9c9c1151a, ; 339: System.Globalization.Calendars => 41
	i64 u0x4f21ee6ef9eb527e, ; 340: ca/Microsoft.Maui.Controls.resources => 324
	i64 u0x4fd5f3ee53d0a4f0, ; 341: SQLitePCLRaw.lib.e_sqlite3.android => 229
	i64 u0x4fdc964ec1888e25, ; 342: lib_Microsoft.Extensions.Configuration.Binder.dll.so => 197
	i64 u0x5037f0be3c28c7a3, ; 343: lib_Microsoft.Maui.Controls.dll.so => 221
	i64 u0x506586cf3ba5b629, ; 344: FokusApp.Presentation => 359
	i64 u0x50c3a29b21050d45, ; 345: System.Linq.Parallel.dll => 62
	i64 u0x5112ed116d87baf8, ; 346: CommunityToolkit.Mvvm => 179
	i64 u0x5116b21580ae6eb0, ; 347: Microsoft.Extensions.Configuration.Binder.dll => 197
	i64 u0x5131bbe80989093f, ; 348: Xamarin.AndroidX.Lifecycle.ViewModel.Android.dll => 278
	i64 u0x516324a5050a7e3c, ; 349: System.Net.WebProxy => 82
	i64 u0x516d6f0b21a303de, ; 350: lib_System.Diagnostics.Contracts.dll.so => 26
	i64 u0x51bb8a2afe774e32, ; 351: System.Drawing => 37
	i64 u0x5247c5c32a4140f0, ; 352: System.Resources.Reader => 102
	i64 u0x526bb15e3c386364, ; 353: Xamarin.AndroidX.Lifecycle.Runtime.Ktx.dll => 275
	i64 u0x526ce79eb8e90527, ; 354: lib_System.Net.Primitives.dll.so => 73
	i64 u0x52829f00b4467c38, ; 355: lib_System.Data.Common.dll.so => 23
	i64 u0x529ffe06f39ab8db, ; 356: Xamarin.AndroidX.Core => 253
	i64 u0x52ff996554dbf352, ; 357: Microsoft.Maui.Graphics => 225
	i64 u0x535f7e40e8fef8af, ; 358: lib-sk-Microsoft.Maui.Controls.resources.dll.so => 348
	i64 u0x53978aac584c666e, ; 359: lib_System.Security.Cryptography.Cng.dll.so => 124
	i64 u0x53a96d5c86c9e194, ; 360: System.Net.NetworkInformation => 71
	i64 u0x53be1038a61e8d44, ; 361: System.Runtime.InteropServices.RuntimeInformation.dll => 110
	i64 u0x53c3014b9437e684, ; 362: lib-zh-HK-Microsoft.Maui.Controls.resources.dll.so => 354
	i64 u0x5435e6f049e9bc37, ; 363: System.Security.Claims.dll => 122
	i64 u0x54795225dd1587af, ; 364: lib_System.Runtime.dll.so => 120
	i64 u0x547a34f14e5f6210, ; 365: Xamarin.AndroidX.Lifecycle.Common.dll => 267
	i64 u0x54b851bc9b470503, ; 366: Xamarin.AndroidX.Navigation.Common.Android => 284
	i64 u0x556e8b63b660ab8b, ; 367: Xamarin.AndroidX.Lifecycle.Common.Jvm.dll => 268
	i64 u0x5588627c9a108ec9, ; 368: System.Collections.Specialized => 12
	i64 u0x55a898e4f42e3fae, ; 369: Microsoft.VisualBasic.Core.dll => 3
	i64 u0x55fa0c610fe93bb1, ; 370: lib_System.Security.Cryptography.OpenSsl.dll.so => 127
	i64 u0x56442b99bc64bb47, ; 371: System.Runtime.Serialization.Xml.dll => 118
	i64 u0x56a8b26e1aeae27b, ; 372: System.Threading.Tasks.Dataflow => 146
	i64 u0x56f932d61e93c07f, ; 373: System.Globalization.Extensions => 42
	i64 u0x571c5cfbec5ae8e2, ; 374: System.Private.Uri => 90
	i64 u0x576499c9f52fea31, ; 375: Xamarin.AndroidX.Annotation => 237
	i64 u0x578cd35c91d7b347, ; 376: lib_SQLitePCLRaw.core.dll.so => 228
	i64 u0x579a06fed6eec900, ; 377: System.Private.CoreLib.dll => 178
	i64 u0x57adda3c951abb33, ; 378: Microsoft.Extensions.Hosting.Abstractions.dll => 210
	i64 u0x57c542c14049b66d, ; 379: System.Diagnostics.DiagnosticSource => 28
	i64 u0x581a8bd5cfda563e, ; 380: System.Threading.Timer => 152
	i64 u0x584ac38e21d2fde1, ; 381: Microsoft.Extensions.Configuration.Binder => 197
	i64 u0x58601b2dda4a27b9, ; 382: lib-ja-Microsoft.Maui.Controls.resources.dll.so => 338
	i64 u0x58688d9af496b168, ; 383: Microsoft.Extensions.DependencyInjection.dll => 200
	i64 u0x588c167a79db6bfb, ; 384: lib_Xamarin.Google.ErrorProne.Annotations.dll.so => 313
	i64 u0x5906028ae5151104, ; 385: Xamarin.AndroidX.Activity.Ktx => 236
	i64 u0x5928b11c2a4b3b58, ; 386: lib_FokusApp.dll.so => 1
	i64 u0x595a356d23e8da9a, ; 387: lib_Microsoft.CSharp.dll.so => 2
	i64 u0x59f9e60b9475085f, ; 388: lib_Xamarin.AndroidX.Annotation.Experimental.dll.so => 238
	i64 u0x5a745f5101a75527, ; 389: lib_System.IO.Compression.FileSystem.dll.so => 45
	i64 u0x5a89a886ae30258d, ; 390: lib_Xamarin.AndroidX.CoordinatorLayout.dll.so => 252
	i64 u0x5a8f6699f4a1caa9, ; 391: lib_System.Threading.dll.so => 153
	i64 u0x5ae9cd33b15841bf, ; 392: System.ComponentModel => 19
	i64 u0x5b54391bdc6fcfe6, ; 393: System.Private.DataContractSerialization => 89
	i64 u0x5b5f0e240a06a2a2, ; 394: da/Microsoft.Maui.Controls.resources.dll => 326
	i64 u0x5b8109e8e14c5e3e, ; 395: System.Globalization.Extensions.dll => 42
	i64 u0x5bddd04d72a9e350, ; 396: Xamarin.AndroidX.Lifecycle.LiveData.Core.Ktx => 271
	i64 u0x5bdf16b09da116ab, ; 397: Xamarin.AndroidX.Collection => 246
	i64 u0x5c019d5266093159, ; 398: lib_Xamarin.AndroidX.Lifecycle.Runtime.Ktx.Android.dll.so => 276
	i64 u0x5c30a4a35f9cc8c4, ; 399: lib_System.Reflection.Extensions.dll.so => 97
	i64 u0x5c393624b8176517, ; 400: lib_Microsoft.Extensions.Logging.dll.so => 213
	i64 u0x5c53c29f5073b0c9, ; 401: System.Diagnostics.FileVersionInfo => 29
	i64 u0x5c87463c575c7616, ; 402: lib_System.Globalization.Extensions.dll.so => 42
	i64 u0x5d0a4a29b02d9d3c, ; 403: System.Net.WebHeaderCollection.dll => 81
	i64 u0x5d25ef991dd9a85c, ; 404: Microsoft.AspNetCore.Components.WebView.Maui.dll => 186
	i64 u0x5d40c9b15181641f, ; 405: lib_Xamarin.AndroidX.Emoji2.dll.so => 261
	i64 u0x5d6ca10d35e9485b, ; 406: lib_Xamarin.AndroidX.Concurrent.Futures.dll.so => 249
	i64 u0x5d7ec76c1c703055, ; 407: System.Threading.Tasks.Parallel => 148
	i64 u0x5db0cbbd1028510e, ; 408: lib_System.Runtime.InteropServices.dll.so => 111
	i64 u0x5db30905d3e5013b, ; 409: Xamarin.AndroidX.Collection.Jvm.dll => 247
	i64 u0x5e467bc8f09ad026, ; 410: System.Collections.Specialized.dll => 12
	i64 u0x5e5173b3208d97e7, ; 411: System.Runtime.Handles.dll => 108
	i64 u0x5ea92fdb19ec8c4c, ; 412: System.Text.Encodings.Web.dll => 140
	i64 u0x5eb8046dd40e9ac3, ; 413: System.ComponentModel.Primitives => 17
	i64 u0x5ec272d219c9aba4, ; 414: System.Security.Cryptography.Csp.dll => 125
	i64 u0x5eee1376d94c7f5e, ; 415: System.Net.HttpListener.dll => 68
	i64 u0x5f36ccf5c6a57e24, ; 416: System.Xml.ReaderWriter.dll => 161
	i64 u0x5f4294b9b63cb842, ; 417: System.Data.Common => 23
	i64 u0x5f7399e166075632, ; 418: lib_SQLitePCLRaw.lib.e_sqlite3.android.dll.so => 229
	i64 u0x5f9a2d823f664957, ; 419: lib-el-Microsoft.Maui.Controls.resources.dll.so => 328
	i64 u0x5fa6da9c3cd8142a, ; 420: lib_Xamarin.KotlinX.Serialization.Core.dll.so => 321
	i64 u0x5fac98e0b37a5b9d, ; 421: System.Runtime.CompilerServices.Unsafe.dll => 105
	i64 u0x609f4b7b63d802d4, ; 422: lib_Microsoft.Extensions.DependencyInjection.dll.so => 200
	i64 u0x60cd4e33d7e60134, ; 423: Xamarin.KotlinX.Coroutines.Core.Jvm => 320
	i64 u0x60f62d786afcf130, ; 424: System.Memory => 65
	i64 u0x61bb78c89f867353, ; 425: System.IO => 59
	i64 u0x61be8d1299194243, ; 426: Microsoft.Maui.Controls.Xaml => 222
	i64 u0x61d2cba29557038f, ; 427: de/Microsoft.Maui.Controls.resources => 327
	i64 u0x61d88f399afb2f45, ; 428: lib_System.Runtime.Loader.dll.so => 113
	i64 u0x622eef6f9e59068d, ; 429: System.Private.CoreLib => 178
	i64 u0x639fb99a7bef11de, ; 430: Xamarin.AndroidX.Navigation.Runtime.Android.dll => 287
	i64 u0x63cdbd66ac39bb46, ; 431: lib_Microsoft.VisualStudio.DesignTools.XamlTapContract.dll.so => 363
	i64 u0x63d5e3aa4ef9b931, ; 432: Xamarin.KotlinX.Coroutines.Android.dll => 318
	i64 u0x63f1f6883c1e23c2, ; 433: lib_System.Collections.Immutable.dll.so => 10
	i64 u0x6400f68068c1e9f1, ; 434: Xamarin.Google.Android.Material.dll => 310
	i64 u0x640e3b14dbd325c2, ; 435: System.Security.Cryptography.Algorithms.dll => 123
	i64 u0x64587004560099b9, ; 436: System.Reflection => 101
	i64 u0x64b1529a438a3c45, ; 437: lib_System.Runtime.Handles.dll.so => 108
	i64 u0x64b61dd9da8a4d57, ; 438: System.Net.ServerSentEvents.dll => 77
	i64 u0x6565fba2cd8f235b, ; 439: Xamarin.AndroidX.Lifecycle.ViewModel.Ktx => 279
	i64 u0x65ecac39144dd3cc, ; 440: Microsoft.Maui.Controls.dll => 221
	i64 u0x65ece51227bfa724, ; 441: lib_System.Runtime.Numerics.dll.so => 114
	i64 u0x661722438787b57f, ; 442: Xamarin.AndroidX.Annotation.Jvm.dll => 239
	i64 u0x6679b2337ee6b22a, ; 443: lib_System.IO.FileSystem.Primitives.dll.so => 50
	i64 u0x667c66a03dd97d40, ; 444: System.Linq.AsyncEnumerable.dll => 60
	i64 u0x6692e924eade1b29, ; 445: lib_System.Console.dll.so => 21
	i64 u0x66a4e5c6a3fb0bae, ; 446: lib_Xamarin.AndroidX.Lifecycle.ViewModel.Android.dll.so => 278
	i64 u0x66d13304ce1a3efa, ; 447: Xamarin.AndroidX.CursorAdapter => 256
	i64 u0x674303f65d8fad6f, ; 448: lib_System.Net.Quic.dll.so => 74
	i64 u0x6756ca4cad62e9d6, ; 449: lib_Xamarin.AndroidX.ConstraintLayout.Core.dll.so => 251
	i64 u0x67c0802770244408, ; 450: System.Windows.dll => 159
	i64 u0x68100b69286e27cd, ; 451: lib_System.Formats.Tar.dll.so => 40
	i64 u0x68558ec653afa616, ; 452: lib-da-Microsoft.Maui.Controls.resources.dll.so => 326
	i64 u0x6857d56b8e8b4bb6, ; 453: lib_Microsoft.AspNetCore.Metadata.dll.so => 187
	i64 u0x6872ec7a2e36b1ac, ; 454: System.Drawing.Primitives.dll => 36
	i64 u0x68fbbbe2eb455198, ; 455: System.Formats.Asn1 => 39
	i64 u0x69063fc0ba8e6bdd, ; 456: he/Microsoft.Maui.Controls.resources.dll => 332
	i64 u0x699dffb2427a2d71, ; 457: SQLitePCLRaw.lib.e_sqlite3.android.dll => 229
	i64 u0x6a4d7577b2317255, ; 458: System.Runtime.InteropServices.dll => 111
	i64 u0x6ace3b74b15ee4a4, ; 459: nb/Microsoft.Maui.Controls.resources => 341
	i64 u0x6afcedb171067e2b, ; 460: System.Core.dll => 22
	i64 u0x6bef98e124147c24, ; 461: Xamarin.Jetbrains.Annotations => 315
	i64 u0x6c46bd19605219e3, ; 462: Microsoft.Extensions.Localization => 211
	i64 u0x6ce874bff138ce2b, ; 463: Xamarin.AndroidX.Lifecycle.ViewModel.dll => 277
	i64 u0x6d12bfaa99c72b1f, ; 464: lib_Microsoft.Maui.Graphics.dll.so => 225
	i64 u0x6d70755158ca866e, ; 465: lib_System.ComponentModel.EventBasedAsync.dll.so => 16
	i64 u0x6d79993361e10ef2, ; 466: Microsoft.Extensions.Primitives => 218
	i64 u0x6d7eeca99577fc8b, ; 467: lib_System.Net.WebProxy.dll.so => 82
	i64 u0x6d8515b19946b6a2, ; 468: System.Net.WebProxy.dll => 82
	i64 u0x6d86d56b84c8eb71, ; 469: lib_Xamarin.AndroidX.CursorAdapter.dll.so => 256
	i64 u0x6d9bea6b3e895cf7, ; 470: Microsoft.Extensions.Primitives.dll => 218
	i64 u0x6e25a02c3833319a, ; 471: lib_Xamarin.AndroidX.Navigation.Fragment.dll.so => 285
	i64 u0x6e79c6bd8627412a, ; 472: Xamarin.AndroidX.SavedState.SavedState.Ktx => 294
	i64 u0x6e838d9a2a6f6c9e, ; 473: lib_System.ValueTuple.dll.so => 156
	i64 u0x6e9965ce1095e60a, ; 474: lib_System.Core.dll.so => 22
	i64 u0x6fd2265da78b93a4, ; 475: lib_Microsoft.Maui.dll.so => 223
	i64 u0x6fdfc7de82c33008, ; 476: cs/Microsoft.Maui.Controls.resources => 325
	i64 u0x6ffc4967cc47ba57, ; 477: System.IO.FileSystem.Watcher.dll => 51
	i64 u0x701cd46a1c25a5fe, ; 478: System.IO.FileSystem.dll => 52
	i64 u0x70d67fb826a6921f, ; 479: Microsoft.Extensions.Validation.dll => 219
	i64 u0x70e99f48c05cb921, ; 480: tr/Microsoft.Maui.Controls.resources.dll => 351
	i64 u0x70fd3deda22442d2, ; 481: lib-nb-Microsoft.Maui.Controls.resources.dll.so => 341
	i64 u0x71485e7ffdb4b958, ; 482: System.Reflection.Extensions => 97
	i64 u0x7162a2fce67a945f, ; 483: lib_Xamarin.Android.Glide.Annotations.dll.so => 232
	i64 u0x717530326f808838, ; 484: lib_Microsoft.Extensions.Diagnostics.Abstractions.dll.so => 204
	i64 u0x71a495ea3761dde8, ; 485: lib-it-Microsoft.Maui.Controls.resources.dll.so => 337
	i64 u0x71ad672adbe48f35, ; 486: System.ComponentModel.Primitives.dll => 17
	i64 u0x720f102581a4a5c8, ; 487: Xamarin.AndroidX.Core.ViewTree => 255
	i64 u0x725f5a9e82a45c81, ; 488: System.Security.Cryptography.Encoding => 126
	i64 u0x72b1fb4109e08d7b, ; 489: lib-hr-Microsoft.Maui.Controls.resources.dll.so => 334
	i64 u0x72e0300099accce1, ; 490: System.Xml.XPath.XDocument => 164
	i64 u0x730bfb248998f67a, ; 491: System.IO.Compression.ZipFile => 46
	i64 u0x732b2d67b9e5c47b, ; 492: Xamarin.Google.ErrorProne.Annotations.dll => 313
	i64 u0x734b76fdc0dc05bb, ; 493: lib_GoogleGson.dll.so => 180
	i64 u0x73a6be34e822f9d1, ; 494: lib_System.Runtime.Serialization.dll.so => 119
	i64 u0x73e4ce94e2eb6ffc, ; 495: lib_System.Memory.dll.so => 65
	i64 u0x73f2645914262879, ; 496: lib_Microsoft.EntityFrameworkCore.Sqlite.dll.so => 192
	i64 u0x743a1eccf080489a, ; 497: WindowsBase.dll => 170
	i64 u0x746cf89b511b4d40, ; 498: lib_Microsoft.Extensions.Diagnostics.dll.so => 203
	i64 u0x755a91767330b3d4, ; 499: lib_Microsoft.Extensions.Configuration.dll.so => 195
	i64 u0x75c326eb821b85c4, ; 500: lib_System.ComponentModel.DataAnnotations.dll.so => 15
	i64 u0x76012e7334db86e5, ; 501: lib_Xamarin.AndroidX.SavedState.dll.so => 292
	i64 u0x76ca07b878f44da0, ; 502: System.Runtime.Numerics.dll => 114
	i64 u0x7736c8a96e51a061, ; 503: lib_Xamarin.AndroidX.Annotation.Jvm.dll.so => 239
	i64 u0x778a805e625329ef, ; 504: System.Linq.Parallel => 62
	i64 u0x779f67ad3b8efbd5, ; 505: Microsoft.Extensions.Configuration.Json.dll => 199
	i64 u0x77d9074d8f33a303, ; 506: lib_System.Net.ServerSentEvents.dll.so => 77
	i64 u0x77f8a4acc2fdc449, ; 507: System.Security.Cryptography.Cng.dll => 124
	i64 u0x780bc73597a503a9, ; 508: lib-ms-Microsoft.Maui.Controls.resources.dll.so => 340
	i64 u0x782c5d8eb99ff201, ; 509: lib_Microsoft.VisualBasic.Core.dll.so => 3
	i64 u0x783606d1e53e7a1a, ; 510: th/Microsoft.Maui.Controls.resources.dll => 350
	i64 u0x78a45e51311409b6, ; 511: Xamarin.AndroidX.Fragment.dll => 264
	i64 u0x78ed4ab8f9d800a1, ; 512: Xamarin.AndroidX.Lifecycle.ViewModel => 277
	i64 u0x7a5207a7c82d30b4, ; 513: lib_Xamarin.JSpecify.dll.so => 316
	i64 u0x7a71889545dcdb00, ; 514: lib_Microsoft.AspNetCore.Components.WebView.dll.so => 185
	i64 u0x7a7e7eddf79c5d26, ; 515: lib_Xamarin.AndroidX.Lifecycle.ViewModel.dll.so => 277
	i64 u0x7a9a57d43b0845fa, ; 516: System.AppContext => 7
	i64 u0x7ad0f4f1e5d08183, ; 517: Xamarin.AndroidX.Collection.dll => 246
	i64 u0x7adb8da2ac89b647, ; 518: fi/Microsoft.Maui.Controls.resources.dll => 330
	i64 u0x7b13d9eaa944ade8, ; 519: Xamarin.AndroidX.DynamicAnimation.dll => 260
	i64 u0x7b150145c0a9058c, ; 520: Microsoft.Data.Sqlite => 188
	i64 u0x7bef86a4335c4870, ; 521: System.ComponentModel.TypeConverter => 18
	i64 u0x7c0820144cd34d6a, ; 522: sk/Microsoft.Maui.Controls.resources.dll => 348
	i64 u0x7c2a0bd1e0f988fc, ; 523: lib-de-Microsoft.Maui.Controls.resources.dll.so => 327
	i64 u0x7c41d387501568ba, ; 524: System.Net.WebClient.dll => 80
	i64 u0x7c482cd79bd24b13, ; 525: lib_Xamarin.AndroidX.ConstraintLayout.dll.so => 250
	i64 u0x7c4867f3cb880d2f, ; 526: Microsoft.AspNetCore.Metadata => 187
	i64 u0x7c60acf6404e96b6, ; 527: Xamarin.AndroidX.Navigation.Common.Android.dll => 284
	i64 u0x7cd2ec8eaf5241cd, ; 528: System.Security.dll => 134
	i64 u0x7cf9ae50dd350622, ; 529: Xamarin.Jetbrains.Annotations.dll => 315
	i64 u0x7d649b75d580bb42, ; 530: ms/Microsoft.Maui.Controls.resources.dll => 340
	i64 u0x7d8b5821548f89e7, ; 531: Microsoft.AspNetCore.Components.Forms => 183
	i64 u0x7d8ee2bdc8e3aad1, ; 532: System.Numerics.Vectors => 86
	i64 u0x7df5df8db8eaa6ac, ; 533: Microsoft.Extensions.Logging.Debug => 215
	i64 u0x7dfc3d6d9d8d7b70, ; 534: System.Collections => 13
	i64 u0x7e2e564fa2f76c65, ; 535: lib_System.Diagnostics.Tracing.dll.so => 35
	i64 u0x7e302e110e1e1346, ; 536: lib_System.Security.Claims.dll.so => 122
	i64 u0x7e4465b3f78ad8d0, ; 537: Xamarin.KotlinX.Serialization.Core.dll => 321
	i64 u0x7e571cad5915e6c3, ; 538: lib_Xamarin.AndroidX.Lifecycle.Process.dll.so => 272
	i64 u0x7e6ac99e4e8df72f, ; 539: System.IO.Hashing => 177
	i64 u0x7e6b1ca712437d7d, ; 540: Xamarin.AndroidX.Emoji2.ViewsHelper => 262
	i64 u0x7e946809d6008ef2, ; 541: lib_System.ObjectModel.dll.so => 88
	i64 u0x7ea0272c1b4a9635, ; 542: lib_Xamarin.Android.Glide.dll.so => 231
	i64 u0x7ecc13347c8fd849, ; 543: lib_System.ComponentModel.dll.so => 19
	i64 u0x7f00ddd9b9ca5a13, ; 544: Xamarin.AndroidX.ViewPager.dll => 305
	i64 u0x7f9351cd44b1273f, ; 545: Microsoft.Extensions.Configuration.Abstractions => 196
	i64 u0x7fbd557c99b3ce6f, ; 546: lib_Xamarin.AndroidX.Lifecycle.LiveData.Core.dll.so => 270
	i64 u0x8076a9a44a2ca331, ; 547: System.Net.Quic => 74
	i64 u0x809b1d586a1c34b9, ; 548: Plugin.Maui.Audio => 226
	i64 u0x80b7e726b0280681, ; 549: Microsoft.VisualStudio.DesignTools.MobileTapContracts => 361
	i64 u0x80da183a87731838, ; 550: System.Reflection.Metadata => 98
	i64 u0x80fa55b6d1b0be99, ; 551: SQLitePCLRaw.provider.e_sqlite3 => 230
	i64 u0x8101a73bd4533440, ; 552: Microsoft.AspNetCore.Components.Web => 184
	i64 u0x812c069d5cdecc17, ; 553: System.dll => 169
	i64 u0x81381be520a60adb, ; 554: Xamarin.AndroidX.Interpolator.dll => 266
	i64 u0x8148a1fb34fceb7c, ; 555: Microsoft.Extensions.Localization.Abstractions => 212
	i64 u0x81657cec2b31e8aa, ; 556: System.Net => 85
	i64 u0x818288c342ac6ea5, ; 557: lib-en-FokusApp.resources.dll.so => 0
	i64 u0x81ab745f6c0f5ce6, ; 558: zh-Hant/Microsoft.Maui.Controls.resources => 356
	i64 u0x8277f2be6b5ce05f, ; 559: Xamarin.AndroidX.AppCompat => 240
	i64 u0x828f06563b30bc50, ; 560: lib_Xamarin.AndroidX.CardView.dll.so => 245
	i64 u0x82b399cb01b531c4, ; 561: lib_System.Web.dll.so => 158
	i64 u0x82df8f5532a10c59, ; 562: lib_System.Drawing.dll.so => 37
	i64 u0x82f0b6e911d13535, ; 563: lib_System.Transactions.dll.so => 155
	i64 u0x82f6403342e12049, ; 564: uk/Microsoft.Maui.Controls.resources => 352
	i64 u0x83c14ba66c8e2b8c, ; 565: zh-Hans/Microsoft.Maui.Controls.resources => 355
	i64 u0x83de69860da6cbdd, ; 566: Microsoft.Extensions.FileProviders.Composite => 206
	i64 u0x846ce984efea52c7, ; 567: System.Threading.Tasks.Parallel.dll => 148
	i64 u0x84ae73148a4557d2, ; 568: lib_System.IO.Pipes.dll.so => 57
	i64 u0x84b01102c12a9232, ; 569: System.Runtime.Serialization.Json.dll => 116
	i64 u0x84cd5cdec0f54bcc, ; 570: lib_Microsoft.EntityFrameworkCore.Relational.dll.so => 191
	i64 u0x850c5ba0b57ce8e7, ; 571: lib_Xamarin.AndroidX.Collection.dll.so => 246
	i64 u0x851d02edd334b044, ; 572: Xamarin.AndroidX.VectorDrawable => 302
	i64 u0x852955234ef97304, ; 573: FokusApp.Presentation.dll => 359
	i64 u0x85c919db62150978, ; 574: Xamarin.AndroidX.Transition.dll => 301
	i64 u0x8662aaeb94fef37f, ; 575: lib_System.Dynamic.Runtime.dll.so => 38
	i64 u0x86a909228dc7657b, ; 576: lib-zh-Hant-Microsoft.Maui.Controls.resources.dll.so => 356
	i64 u0x86b3e00c36b84509, ; 577: Microsoft.Extensions.Configuration.dll => 195
	i64 u0x86b62cb077ec4fd7, ; 578: System.Runtime.Serialization.Xml => 118
	i64 u0x8704193f462e892e, ; 579: lib_Microsoft.Extensions.FileSystemGlobbing.dll.so => 209
	i64 u0x8706ffb12bf3f53d, ; 580: Xamarin.AndroidX.Annotation.Experimental => 238
	i64 u0x872a5b14c18d328c, ; 581: System.ComponentModel.DataAnnotations => 15
	i64 u0x872fb9615bc2dff0, ; 582: Xamarin.Android.Glide.Annotations.dll => 232
	i64 u0x87c4b8a492b176ad, ; 583: Microsoft.EntityFrameworkCore.Abstractions => 190
	i64 u0x87c69b87d9283884, ; 584: lib_System.Threading.Thread.dll.so => 150
	i64 u0x87f6569b25707834, ; 585: System.IO.Compression.Brotli.dll => 44
	i64 u0x8842b3a5d2d3fb36, ; 586: Microsoft.Maui.Essentials => 224
	i64 u0x88926583efe7ee86, ; 587: Xamarin.AndroidX.Activity.Ktx.dll => 236
	i64 u0x88ba6bc4f7762b03, ; 588: lib_System.Reflection.dll.so => 101
	i64 u0x88bda98e0cffb7a9, ; 589: lib_Xamarin.KotlinX.Coroutines.Core.Jvm.dll.so => 320
	i64 u0x8930322c7bd8f768, ; 590: netstandard => 172
	i64 u0x897a606c9e39c75f, ; 591: lib_System.ComponentModel.Primitives.dll.so => 17
	i64 u0x898a5c6bc9e47ec1, ; 592: lib_Xamarin.AndroidX.SavedState.SavedState.Android.dll.so => 293
	i64 u0x89911a22005b92b7, ; 593: System.IO.FileSystem.DriveInfo.dll => 49
	i64 u0x89c5188089ec2cd5, ; 594: lib_System.Runtime.InteropServices.RuntimeInformation.dll.so => 110
	i64 u0x8a19e3dc71b34b2c, ; 595: System.Reflection.TypeExtensions.dll => 100
	i64 u0x8a399a706fcbce4b, ; 596: Microsoft.Extensions.Caching.Abstractions => 193
	i64 u0x8ad229ea26432ee2, ; 597: Xamarin.AndroidX.Loader => 282
	i64 u0x8b4ff5d0fdd5faa1, ; 598: lib_System.Diagnostics.DiagnosticSource.dll.so => 28
	i64 u0x8b541d476eb3774c, ; 599: System.Security.Principal.Windows => 131
	i64 u0x8b8d01333a96d0b5, ; 600: System.Diagnostics.Process.dll => 30
	i64 u0x8b9ceca7acae3451, ; 601: lib-he-Microsoft.Maui.Controls.resources.dll.so => 332
	i64 u0x8c575135aa1ccef4, ; 602: Microsoft.Extensions.FileProviders.Abstractions => 205
	i64 u0x8cb8f612b633affb, ; 603: Xamarin.AndroidX.SavedState.SavedState.Ktx.dll => 294
	i64 u0x8cdfdb4ce85fb925, ; 604: lib_System.Security.Principal.Windows.dll.so => 131
	i64 u0x8cdfe7b8f4caa426, ; 605: System.IO.Compression.FileSystem => 45
	i64 u0x8d0f420977c2c1c7, ; 606: Xamarin.AndroidX.CursorAdapter.dll => 256
	i64 u0x8d52a25632e81824, ; 607: Microsoft.EntityFrameworkCore.Sqlite.dll => 192
	i64 u0x8d52f7ea2796c531, ; 608: Xamarin.AndroidX.Emoji2.dll => 261
	i64 u0x8d7b8ab4b3310ead, ; 609: System.Threading => 153
	i64 u0x8da188285aadfe8e, ; 610: System.Collections.Concurrent => 9
	i64 u0x8e8f269ad1e1ff94, ; 611: lib_Xamarin.AndroidX.Tracing.Tracing.Android.dll.so => 300
	i64 u0x8ed807bfe9858dfc, ; 612: Xamarin.AndroidX.Navigation.Common => 283
	i64 u0x8ee08b8194a30f48, ; 613: lib-hi-Microsoft.Maui.Controls.resources.dll.so => 333
	i64 u0x8ef7601039857a44, ; 614: lib-ro-Microsoft.Maui.Controls.resources.dll.so => 346
	i64 u0x8ef9414937d93a0a, ; 615: SQLitePCLRaw.core.dll => 228
	i64 u0x8f32c6f611f6ffab, ; 616: pt/Microsoft.Maui.Controls.resources.dll => 345
	i64 u0x8f44b45eb046bbd1, ; 617: System.ServiceModel.Web.dll => 135
	i64 u0x8f8829d21c8985a4, ; 618: lib-pt-BR-Microsoft.Maui.Controls.resources.dll.so => 344
	i64 u0x8fbf5b0114c6dcef, ; 619: System.Globalization.dll => 43
	i64 u0x8fcc8c2a81f3d9e7, ; 620: Xamarin.KotlinX.Serialization.Core => 321
	i64 u0x8fd27d934d7b3a55, ; 621: SQLitePCLRaw.core => 228
	i64 u0x90263f8448b8f572, ; 622: lib_System.Diagnostics.TraceSource.dll.so => 34
	i64 u0x903101b46fb73a04, ; 623: _Microsoft.Android.Resource.Designer => 364
	i64 u0x90393bd4865292f3, ; 624: lib_System.IO.Compression.dll.so => 47
	i64 u0x905e2b8e7ae91ae6, ; 625: System.Threading.Tasks.Extensions.dll => 147
	i64 u0x90634f86c5ebe2b5, ; 626: Xamarin.AndroidX.Lifecycle.ViewModel.Android => 278
	i64 u0x907b636704ad79ef, ; 627: lib_Microsoft.Maui.Controls.Xaml.dll.so => 222
	i64 u0x90e9efbfd68593e0, ; 628: lib_Xamarin.AndroidX.Lifecycle.LiveData.dll.so => 269
	i64 u0x91418dc638b29e68, ; 629: lib_Xamarin.AndroidX.CustomView.dll.so => 257
	i64 u0x914647982e998267, ; 630: Microsoft.Extensions.Configuration.Json => 199
	i64 u0x9157bd523cd7ed36, ; 631: lib_System.Text.Json.dll.so => 141
	i64 u0x91a74f07b30d37e2, ; 632: System.Linq.dll => 64
	i64 u0x91cb86ea3b17111d, ; 633: System.ServiceModel.Web => 135
	i64 u0x91fa41a87223399f, ; 634: ca/Microsoft.Maui.Controls.resources.dll => 324
	i64 u0x92054e486c0c7ea7, ; 635: System.IO.FileSystem.DriveInfo => 49
	i64 u0x928614058c40c4cd, ; 636: lib_System.Xml.XPath.XDocument.dll.so => 164
	i64 u0x92b138fffca2b01e, ; 637: lib_Xamarin.AndroidX.Arch.Core.Runtime.dll.so => 243
	i64 u0x92dfc2bfc6c6a888, ; 638: Xamarin.AndroidX.Lifecycle.LiveData => 269
	i64 u0x933da2c779423d68, ; 639: Xamarin.Android.Glide.Annotations => 232
	i64 u0x9388aad9b7ae40ce, ; 640: lib_Xamarin.AndroidX.Lifecycle.Common.dll.so => 267
	i64 u0x93cfa73ab28d6e35, ; 641: ms/Microsoft.Maui.Controls.resources => 340
	i64 u0x941c00d21e5c0679, ; 642: lib_Xamarin.AndroidX.Transition.dll.so => 301
	i64 u0x944077d8ca3c6580, ; 643: System.IO.Compression.dll => 47
	i64 u0x948cffedc8ed7960, ; 644: System.Xml => 168
	i64 u0x94bbeab0d4764588, ; 645: System.IO.Hashing.dll => 177
	i64 u0x94c8990839c4bdb1, ; 646: lib_Xamarin.AndroidX.Interpolator.dll.so => 266
	i64 u0x94e68b2add1bcd0d, ; 647: en/FokusApp.resources => 0
	i64 u0x967fc325e09bfa8c, ; 648: es/Microsoft.Maui.Controls.resources => 329
	i64 u0x9686161486d34b81, ; 649: lib_Xamarin.AndroidX.ExifInterface.dll.so => 263
	i64 u0x9732d8dbddea3d9a, ; 650: id/Microsoft.Maui.Controls.resources => 336
	i64 u0x978be80e5210d31b, ; 651: Microsoft.Maui.Graphics.dll => 225
	i64 u0x97b8c771ea3e4220, ; 652: System.ComponentModel.dll => 19
	i64 u0x97e144c9d3c6976e, ; 653: System.Collections.Concurrent.dll => 9
	i64 u0x984184e3c70d4419, ; 654: GoogleGson => 180
	i64 u0x9843944103683dd3, ; 655: Xamarin.AndroidX.Core.Core.Ktx => 254
	i64 u0x98b05cc81e6f333c, ; 656: Xamarin.AndroidX.SavedState.SavedState.Android.dll => 293
	i64 u0x98d720cc4597562c, ; 657: System.Security.Cryptography.OpenSsl => 127
	i64 u0x991d510397f92d9d, ; 658: System.Linq.Expressions => 61
	i64 u0x996ceeb8a3da3d67, ; 659: System.Threading.Overlapped.dll => 145
	i64 u0x999cb19e1a04ffd3, ; 660: CommunityToolkit.Mvvm.dll => 179
	i64 u0x99a00ca5270c6878, ; 661: Xamarin.AndroidX.Navigation.Runtime => 286
	i64 u0x99cdc6d1f2d3a72f, ; 662: ko/Microsoft.Maui.Controls.resources.dll => 339
	i64 u0x9a01b1da98b6ee10, ; 663: Xamarin.AndroidX.Lifecycle.Runtime.dll => 273
	i64 u0x9a5ccc274fd6e6ee, ; 664: Jsr305Binding.dll => 311
	i64 u0x9ae6940b11c02876, ; 665: lib_Xamarin.AndroidX.Window.dll.so => 307
	i64 u0x9b211a749105beac, ; 666: System.Transactions.Local => 154
	i64 u0x9b8734714671022d, ; 667: System.Threading.Tasks.Dataflow.dll => 146
	i64 u0x9bc6aea27fbf034f, ; 668: lib_Xamarin.KotlinX.Coroutines.Core.dll.so => 319
	i64 u0x9c244ac7cda32d26, ; 669: System.Security.Cryptography.X509Certificates.dll => 129
	i64 u0x9c465f280cf43733, ; 670: lib_Xamarin.KotlinX.Coroutines.Android.dll.so => 318
	i64 u0x9c8f6872beab6408, ; 671: System.Xml.XPath.XDocument.dll => 164
	i64 u0x9ce01cf91101ae23, ; 672: System.Xml.XmlDocument => 166
	i64 u0x9d128180c81d7ce6, ; 673: Xamarin.AndroidX.CustomView.PoolingContainer => 258
	i64 u0x9d1f6cd40094e12b, ; 674: lib_FokusApp.Core.dll.so => 357
	i64 u0x9d5dbcf5a48583fe, ; 675: lib_Xamarin.AndroidX.Activity.dll.so => 235
	i64 u0x9d74dee1a7725f34, ; 676: Microsoft.Extensions.Configuration.Abstractions.dll => 196
	i64 u0x9dd0e195825d65c6, ; 677: lib_Xamarin.AndroidX.Navigation.Runtime.Android.dll.so => 287
	i64 u0x9e4534b6adaf6e84, ; 678: nl/Microsoft.Maui.Controls.resources => 342
	i64 u0x9e4b95dec42769f7, ; 679: System.Diagnostics.Debug.dll => 27
	i64 u0x9eaf1efdf6f7267e, ; 680: Xamarin.AndroidX.Navigation.Common.dll => 283
	i64 u0x9ef542cf1f78c506, ; 681: Xamarin.AndroidX.Lifecycle.LiveData.Core => 270
	i64 u0x9fbb2961ca18e5c2, ; 682: Microsoft.Extensions.FileProviders.Physical.dll => 208
	i64 u0xa00832eb975f56a8, ; 683: lib_System.Net.dll.so => 85
	i64 u0xa0ad78236b7b267f, ; 684: Xamarin.AndroidX.Window => 307
	i64 u0xa0d8259f4cc284ec, ; 685: lib_System.Security.Cryptography.dll.so => 130
	i64 u0xa0e17ca50c77a225, ; 686: lib_Xamarin.Google.Crypto.Tink.Android.dll.so => 312
	i64 u0xa0ff9b3e34d92f11, ; 687: lib_System.Resources.Writer.dll.so => 104
	i64 u0xa12fbfb4da97d9f3, ; 688: System.Threading.Timer.dll => 152
	i64 u0xa1440773ee9d341e, ; 689: Xamarin.Google.Android.Material => 310
	i64 u0xa18c39c44cdc3465, ; 690: Xamarin.AndroidX.Window.WindowCore => 308
	i64 u0xa1b9d7c27f47219f, ; 691: Xamarin.AndroidX.Navigation.UI.dll => 288
	i64 u0xa2572680829d2c7c, ; 692: System.IO.Pipelines.dll => 55
	i64 u0xa26597e57ee9c7f6, ; 693: System.Xml.XmlDocument.dll => 166
	i64 u0xa308401900e5bed3, ; 694: lib_mscorlib.dll.so => 171
	i64 u0xa395572e7da6c99d, ; 695: lib_System.Security.dll.so => 134
	i64 u0xa3b8104115a36bf6, ; 696: lib_Microsoft.Extensions.FileProviders.Embedded.dll.so => 207
	i64 u0xa3e683f24b43af6f, ; 697: System.Dynamic.Runtime.dll => 38
	i64 u0xa4145becdee3dc4f, ; 698: Xamarin.AndroidX.VectorDrawable.Animated => 303
	i64 u0xa46aa1eaa214539b, ; 699: ko/Microsoft.Maui.Controls.resources => 339
	i64 u0xa4a372eecb9e4df0, ; 700: Microsoft.Extensions.Diagnostics => 203
	i64 u0xa4d20d2ff0563d26, ; 701: lib_CommunityToolkit.Mvvm.dll.so => 179
	i64 u0xa4e62983cf1e3674, ; 702: Microsoft.AspNetCore.Components.Forms.dll => 183
	i64 u0xa4edc8f2ceae241a, ; 703: System.Data.Common.dll => 23
	i64 u0xa5494f40f128ce6a, ; 704: System.Runtime.Serialization.Formatters.dll => 115
	i64 u0xa54b74df83dce92b, ; 705: System.Reflection.DispatchProxy => 93
	i64 u0xa5b7152421ed6d98, ; 706: lib_System.IO.FileSystem.Watcher.dll.so => 51
	i64 u0xa5c3844f17b822db, ; 707: lib_System.Linq.Parallel.dll.so => 62
	i64 u0xa5ce5c755bde8cb8, ; 708: lib_System.Security.Cryptography.Csp.dll.so => 125
	i64 u0xa5e599d1e0524750, ; 709: System.Numerics.Vectors.dll => 86
	i64 u0xa5f1ba49b85dd355, ; 710: System.Security.Cryptography.dll => 130
	i64 u0xa61975a5a37873ea, ; 711: lib_System.Xml.XmlSerializer.dll.so => 167
	i64 u0xa6593e21584384d2, ; 712: lib_Jsr305Binding.dll.so => 311
	i64 u0xa66cbee0130865f7, ; 713: lib_WindowsBase.dll.so => 170
	i64 u0xa67dbee13e1df9ca, ; 714: Xamarin.AndroidX.SavedState.dll => 292
	i64 u0xa684b098dd27b296, ; 715: lib_Xamarin.AndroidX.Security.SecurityCrypto.dll.so => 295
	i64 u0xa68a420042bb9b1f, ; 716: Xamarin.AndroidX.DrawerLayout.dll => 259
	i64 u0xa6d26156d1cacc7c, ; 717: Xamarin.Android.Glide.dll => 231
	i64 u0xa75386b5cb9595aa, ; 718: Xamarin.AndroidX.Lifecycle.Runtime.Android => 274
	i64 u0xa763fbb98df8d9fb, ; 719: lib_Microsoft.Win32.Primitives.dll.so => 5
	i64 u0xa78ce3745383236a, ; 720: Xamarin.AndroidX.Lifecycle.Common.Jvm => 268
	i64 u0xa7c31b56b4dc7b33, ; 721: hu/Microsoft.Maui.Controls.resources => 335
	i64 u0xa7eab29ed44b4e7a, ; 722: Mono.Android.Export => 174
	i64 u0xa8195217cbf017b7, ; 723: Microsoft.VisualBasic.Core => 3
	i64 u0xa82fd211eef00a5b, ; 724: Microsoft.Extensions.FileProviders.Physical => 208
	i64 u0xa859a95830f367ff, ; 725: lib_Xamarin.AndroidX.Lifecycle.ViewModel.Ktx.dll.so => 279
	i64 u0xa8b52f21e0dbe690, ; 726: System.Runtime.Serialization.dll => 119
	i64 u0xa8c84ce526c2b4bd, ; 727: Microsoft.VisualStudio.DesignTools.XamlTapContract.dll => 363
	i64 u0xa8ee4ed7de2efaee, ; 728: Xamarin.AndroidX.Annotation.dll => 237
	i64 u0xa95590e7c57438a4, ; 729: System.Configuration => 20
	i64 u0xaa2219c8e3449ff5, ; 730: Microsoft.Extensions.Logging.Abstractions => 214
	i64 u0xaa443ac34067eeef, ; 731: System.Private.Xml.dll => 92
	i64 u0xaa52de307ef5d1dd, ; 732: System.Net.Http => 67
	i64 u0xaa9a7b0214a5cc5c, ; 733: System.Diagnostics.StackTrace.dll => 31
	i64 u0xaaaf86367285a918, ; 734: Microsoft.Extensions.DependencyInjection.Abstractions.dll => 201
	i64 u0xaaf84bb3f052a265, ; 735: el/Microsoft.Maui.Controls.resources => 328
	i64 u0xab9af77b5b67a0b8, ; 736: Xamarin.AndroidX.ConstraintLayout.Core => 251
	i64 u0xab9c1b2687d86b0b, ; 737: lib_System.Linq.Expressions.dll.so => 61
	i64 u0xac2af3fa195a15ce, ; 738: System.Runtime.Numerics => 114
	i64 u0xac5376a2a538dc10, ; 739: Xamarin.AndroidX.Lifecycle.LiveData.Core.dll => 270
	i64 u0xac5acae88f60357e, ; 740: System.Diagnostics.Tools.dll => 33
	i64 u0xac79c7e46047ad98, ; 741: System.Security.Principal.Windows.dll => 131
	i64 u0xac98d31068e24591, ; 742: System.Xml.XDocument => 163
	i64 u0xacd46e002c3ccb97, ; 743: ro/Microsoft.Maui.Controls.resources => 346
	i64 u0xacdd9e4180d56dda, ; 744: Xamarin.AndroidX.Concurrent.Futures => 249
	i64 u0xacf42eea7ef9cd12, ; 745: System.Threading.Channels => 144
	i64 u0xad89c07347f1bad6, ; 746: nl/Microsoft.Maui.Controls.resources.dll => 342
	i64 u0xadbb53caf78a79d2, ; 747: System.Web.HttpUtility => 157
	i64 u0xadc90ab061a9e6e4, ; 748: System.ComponentModel.TypeConverter.dll => 18
	i64 u0xadca1b9030b9317e, ; 749: Xamarin.AndroidX.Collection.Ktx => 248
	i64 u0xadd8eda2edf396ad, ; 750: Xamarin.Android.Glide.GifDecoder => 234
	i64 u0xadf4cf30debbeb9a, ; 751: System.Net.ServicePoint.dll => 78
	i64 u0xadf511667bef3595, ; 752: System.Net.Security => 76
	i64 u0xae0aaa94fdcfce0f, ; 753: System.ComponentModel.EventBasedAsync.dll => 16
	i64 u0xae282bcd03739de7, ; 754: Java.Interop => 173
	i64 u0xae53579c90db1107, ; 755: System.ObjectModel.dll => 88
	i64 u0xaf12fb8133ac3fbb, ; 756: Microsoft.EntityFrameworkCore.Sqlite => 192
	i64 u0xaf732d0b2193b8f5, ; 757: System.Security.Cryptography.OpenSsl.dll => 127
	i64 u0xafdb94dbccd9d11c, ; 758: Xamarin.AndroidX.Lifecycle.LiveData.dll => 269
	i64 u0xafe29f45095518e7, ; 759: lib_Xamarin.AndroidX.Lifecycle.ViewModelSavedState.dll.so => 280
	i64 u0xb03ae931fb25607e, ; 760: Xamarin.AndroidX.ConstraintLayout => 250
	i64 u0xb05cc42cd94c6d9d, ; 761: lib-sv-Microsoft.Maui.Controls.resources.dll.so => 349
	i64 u0xb0ac21bec8f428c5, ; 762: Xamarin.AndroidX.Lifecycle.Runtime.Ktx.Android.dll => 276
	i64 u0xb0bb43dc52ea59f9, ; 763: System.Diagnostics.Tracing.dll => 35
	i64 u0xb1ccbf6243328d1c, ; 764: Microsoft.AspNetCore.Components => 182
	i64 u0xb1dd05401aa8ee63, ; 765: System.Security.AccessControl => 121
	i64 u0xb220631954820169, ; 766: System.Text.RegularExpressions => 142
	i64 u0xb2376e1dbf8b4ed7, ; 767: System.Security.Cryptography.Csp => 125
	i64 u0xb2a1959fe95c5402, ; 768: lib_System.Runtime.InteropServices.JavaScript.dll.so => 109
	i64 u0xb2a3f67f3bf29fce, ; 769: da/Microsoft.Maui.Controls.resources => 326
	i64 u0xb3011a0a57f7ffb2, ; 770: Microsoft.VisualStudio.DesignTools.MobileTapContracts.dll => 361
	i64 u0xb3874072ee0ecf8c, ; 771: Xamarin.AndroidX.VectorDrawable.Animated.dll => 303
	i64 u0xb3f0a0fcda8d3ebc, ; 772: Xamarin.AndroidX.CardView => 245
	i64 u0xb46be1aa6d4fff93, ; 773: hi/Microsoft.Maui.Controls.resources => 333
	i64 u0xb477491be13109d8, ; 774: ar/Microsoft.Maui.Controls.resources => 323
	i64 u0xb4bd7015ecee9d86, ; 775: System.IO.Pipelines => 55
	i64 u0xb4c53d9749c5f226, ; 776: lib_System.IO.FileSystem.AccessControl.dll.so => 48
	i64 u0xb4ff710863453fda, ; 777: System.Diagnostics.FileVersionInfo.dll => 29
	i64 u0xb54092076b15e062, ; 778: System.Threading.AccessControl => 143
	i64 u0xb5c38bf497a4cfe2, ; 779: lib_System.Threading.Tasks.dll.so => 149
	i64 u0xb5c7fcdafbc67ee4, ; 780: Microsoft.Extensions.Logging.Abstractions.dll => 214
	i64 u0xb5ea31d5244c6626, ; 781: System.Threading.ThreadPool.dll => 151
	i64 u0xb7212c4683a94afe, ; 782: System.Drawing.Primitives => 36
	i64 u0xb7b7753d1f319409, ; 783: sv/Microsoft.Maui.Controls.resources => 349
	i64 u0xb81a2c6e0aee50fe, ; 784: lib_System.Private.CoreLib.dll.so => 178
	i64 u0xb8c60af47c08d4da, ; 785: System.Net.ServicePoint => 78
	i64 u0xb8e68d20aad91196, ; 786: lib_System.Xml.XPath.dll.so => 165
	i64 u0xb9185c33a1643eed, ; 787: Microsoft.CSharp.dll => 2
	i64 u0xb960d6b2200ba320, ; 788: Xamarin.AndroidX.Lifecycle.ViewModelSavedState.Android.dll => 281
	i64 u0xb9b8001adf4ed7cc, ; 789: lib_Xamarin.AndroidX.SlidingPaneLayout.dll.so => 296
	i64 u0xb9f64d3b230def68, ; 790: lib-pt-Microsoft.Maui.Controls.resources.dll.so => 345
	i64 u0xb9fc3c8a556e3691, ; 791: ja/Microsoft.Maui.Controls.resources => 338
	i64 u0xba4670aa94a2b3c6, ; 792: lib_System.Xml.XDocument.dll.so => 163
	i64 u0xba48785529705af9, ; 793: System.Collections.dll => 13
	i64 u0xba965b8c86359996, ; 794: lib_System.Windows.dll.so => 159
	i64 u0xbaf762c4825c14e9, ; 795: Microsoft.AspNetCore.Components.WebView => 185
	i64 u0xbb286883bc35db36, ; 796: System.Transactions.dll => 155
	i64 u0xbb65706fde942ce3, ; 797: System.Net.Sockets => 79
	i64 u0xbba28979413cad9e, ; 798: lib_System.Runtime.CompilerServices.VisualC.dll.so => 106
	i64 u0xbbd180354b67271a, ; 799: System.Runtime.Serialization.Formatters => 115
	i64 u0xbc22a245dab70cb4, ; 800: lib_SQLitePCLRaw.provider.e_sqlite3.dll.so => 230
	i64 u0xbc260cdba33291a3, ; 801: Xamarin.AndroidX.Arch.Core.Common.dll => 242
	i64 u0xbc3c4e8dffea9d4e, ; 802: Microsoft.AspNetCore.Metadata.dll => 187
	i64 u0xbcd36316d29f27b4, ; 803: lib_Microsoft.AspNetCore.Authorization.dll.so => 181
	i64 u0xbd0e2c0d55246576, ; 804: System.Net.Http.dll => 67
	i64 u0xbd3fbd85b9e1cb29, ; 805: lib_System.Net.HttpListener.dll.so => 68
	i64 u0xbd437a2cdb333d0d, ; 806: Xamarin.AndroidX.ViewPager2 => 306
	i64 u0xbd4f572d2bd0a789, ; 807: System.IO.Compression.ZipFile.dll => 46
	i64 u0xbd5d0b88d3d647a5, ; 808: lib_Xamarin.AndroidX.Browser.dll.so => 244
	i64 u0xbd877b14d0b56392, ; 809: System.Runtime.Intrinsics.dll => 112
	i64 u0xbe65a49036345cf4, ; 810: lib_System.Buffers.dll.so => 8
	i64 u0xbee38d4a88835966, ; 811: Xamarin.AndroidX.AppCompat.AppCompatResources => 241
	i64 u0xbef9919db45b4ca7, ; 812: System.IO.Pipes.AccessControl => 56
	i64 u0xbf0fa68611139208, ; 813: lib_Xamarin.AndroidX.Annotation.dll.so => 237
	i64 u0xbfc1e1fb3095f2b3, ; 814: lib_System.Net.Http.Json.dll.so => 66
	i64 u0xc040a4ab55817f58, ; 815: ar/Microsoft.Maui.Controls.resources.dll => 323
	i64 u0xc07cadab29efeba0, ; 816: Xamarin.AndroidX.Core.Core.Ktx.dll => 254
	i64 u0xc0d928351ab5ca77, ; 817: System.Console.dll => 21
	i64 u0xc0f5a221a9383aea, ; 818: System.Runtime.Intrinsics => 112
	i64 u0xc111030af54d7191, ; 819: System.Resources.Writer => 104
	i64 u0xc12b8b3afa48329c, ; 820: lib_System.Linq.dll.so => 64
	i64 u0xc183ca0b74453aa9, ; 821: lib_System.Threading.Tasks.Dataflow.dll.so => 146
	i64 u0xc1c2cb7af77b8858, ; 822: Microsoft.EntityFrameworkCore => 189
	i64 u0xc1ebdc7e6a943450, ; 823: Microsoft.AspNetCore.Authorization.dll => 181
	i64 u0xc1ff9ae3cdb6e1e6, ; 824: Xamarin.AndroidX.Activity.dll => 235
	i64 u0xc26c064effb1dea9, ; 825: System.Buffers.dll => 8
	i64 u0xc28c50f32f81cc73, ; 826: ja/Microsoft.Maui.Controls.resources.dll => 338
	i64 u0xc2902f6cf5452577, ; 827: lib_Mono.Android.Export.dll.so => 174
	i64 u0xc2a3bca55b573141, ; 828: System.IO.FileSystem.Watcher => 51
	i64 u0xc2bcfec99f69365e, ; 829: Xamarin.AndroidX.ViewPager2.dll => 306
	i64 u0xc30b52815b58ac2c, ; 830: lib_System.Runtime.Serialization.Xml.dll.so => 118
	i64 u0xc3492f8f90f96ce4, ; 831: lib_Microsoft.Extensions.DependencyModel.dll.so => 202
	i64 u0xc36d7d89c652f455, ; 832: System.Threading.Overlapped => 145
	i64 u0xc396b285e59e5493, ; 833: GoogleGson.dll => 180
	i64 u0xc3c86c1e5e12f03d, ; 834: WindowsBase => 170
	i64 u0xc3cc416033a7b1b7, ; 835: FokusApp => 1
	i64 u0xc421b61fd853169d, ; 836: lib_System.Net.WebSockets.Client.dll.so => 83
	i64 u0xc463e077917aa21d, ; 837: System.Runtime.Serialization.Json => 116
	i64 u0xc472ce300460ccb6, ; 838: Microsoft.EntityFrameworkCore.dll => 189
	i64 u0xc4d3858ed4d08512, ; 839: Xamarin.AndroidX.Lifecycle.ViewModelSavedState.dll => 280
	i64 u0xc4d69851fe06342f, ; 840: lib_Microsoft.Extensions.Caching.Memory.dll.so => 194
	i64 u0xc50fded0ded1418c, ; 841: lib_System.ComponentModel.TypeConverter.dll.so => 18
	i64 u0xc519125d6bc8fb11, ; 842: lib_System.Net.Requests.dll.so => 75
	i64 u0xc5293b19e4dc230e, ; 843: Xamarin.AndroidX.Navigation.Fragment => 285
	i64 u0xc5325b2fcb37446f, ; 844: lib_System.Private.Xml.dll.so => 92
	i64 u0xc535cb9a21385d9b, ; 845: lib_Xamarin.Android.Glide.DiskLruCache.dll.so => 233
	i64 u0xc5a0f4b95a699af7, ; 846: lib_System.Private.Uri.dll.so => 90
	i64 u0xc5cdcd5b6277579e, ; 847: lib_System.Security.Cryptography.Algorithms.dll.so => 123
	i64 u0xc5ec286825cb0bf4, ; 848: Xamarin.AndroidX.Tracing.Tracing => 299
	i64 u0xc659b586d4c229e2, ; 849: Microsoft.Extensions.Configuration.FileExtensions.dll => 198
	i64 u0xc6706bc8aa7fe265, ; 850: Xamarin.AndroidX.Annotation.Jvm => 239
	i64 u0xc74d70d4aa96cef3, ; 851: Xamarin.AndroidX.Navigation.Runtime.Android => 287
	i64 u0xc7c01e7d7c93a110, ; 852: System.Text.Encoding.Extensions.dll => 138
	i64 u0xc7ce851898a4548e, ; 853: lib_System.Web.HttpUtility.dll.so => 157
	i64 u0xc809d4089d2556b2, ; 854: System.Runtime.InteropServices.JavaScript.dll => 109
	i64 u0xc858a28d9ee5a6c5, ; 855: lib_System.Collections.Specialized.dll.so => 12
	i64 u0xc8ac7c6bf1c2ec51, ; 856: System.Reflection.DispatchProxy.dll => 93
	i64 u0xc9c62c8f354ac568, ; 857: lib_System.Diagnostics.TextWriterTraceListener.dll.so => 32
	i64 u0xca3110fea81c8916, ; 858: Microsoft.AspNetCore.Components.Web.dll => 184
	i64 u0xca32340d8d54dcd5, ; 859: Microsoft.Extensions.Caching.Memory.dll => 194
	i64 u0xca3a723e7342c5b6, ; 860: lib-tr-Microsoft.Maui.Controls.resources.dll.so => 351
	i64 u0xca5801070d9fccfb, ; 861: System.Text.Encoding => 139
	i64 u0xcab3493c70141c2d, ; 862: pl/Microsoft.Maui.Controls.resources => 343
	i64 u0xcacfddc9f7c6de76, ; 863: ro/Microsoft.Maui.Controls.resources.dll => 346
	i64 u0xcadbc92899a777f0, ; 864: Xamarin.AndroidX.Startup.StartupRuntime => 297
	i64 u0xcb45618372c47127, ; 865: Microsoft.EntityFrameworkCore.Relational => 191
	i64 u0xcba1cb79f45292b5, ; 866: Xamarin.Android.Glide.GifDecoder.dll => 234
	i64 u0xcbb5f80c7293e696, ; 867: lib_System.Globalization.Calendars.dll.so => 41
	i64 u0xcbd4fdd9cef4a294, ; 868: lib__Microsoft.Android.Resource.Designer.dll.so => 364
	i64 u0xcc15da1e07bbd994, ; 869: Xamarin.AndroidX.SlidingPaneLayout => 296
	i64 u0xcc2876b32ef2794c, ; 870: lib_System.Text.RegularExpressions.dll.so => 142
	i64 u0xcc5c3bb714c4561e, ; 871: Xamarin.KotlinX.Coroutines.Core.Jvm.dll => 320
	i64 u0xcc76886e09b88260, ; 872: Xamarin.KotlinX.Serialization.Core.Jvm.dll => 322
	i64 u0xcc9fa2923aa1c9ef, ; 873: System.Diagnostics.Contracts.dll => 26
	i64 u0xccae9bb73e2326bd, ; 874: lib_System.IO.Hashing.dll.so => 177
	i64 u0xccf25c4b634ccd3a, ; 875: zh-Hans/Microsoft.Maui.Controls.resources.dll => 355
	i64 u0xcd10a42808629144, ; 876: System.Net.Requests => 75
	i64 u0xcdca1b920e9f53ba, ; 877: Xamarin.AndroidX.Interpolator => 266
	i64 u0xcdd0c48b6937b21c, ; 878: Xamarin.AndroidX.SwipeRefreshLayout => 298
	i64 u0xcde1fa22dc303670, ; 879: Microsoft.VisualStudio.DesignTools.XamlTapContract => 363
	i64 u0xcf23d8093f3ceadf, ; 880: System.Diagnostics.DiagnosticSource.dll => 28
	i64 u0xcf5ff6b6b2c4c382, ; 881: System.Net.Mail.dll => 69
	i64 u0xcf8fc898f98b0d34, ; 882: System.Private.Xml.Linq => 91
	i64 u0xd04b5f59ed596e31, ; 883: System.Reflection.Metadata.dll => 98
	i64 u0xd063299fcfc0c93f, ; 884: lib_System.Runtime.Serialization.Json.dll.so => 116
	i64 u0xd0de8a113e976700, ; 885: System.Diagnostics.TextWriterTraceListener => 32
	i64 u0xd0fc33d5ae5d4cb8, ; 886: System.Runtime.Extensions => 107
	i64 u0xd1194e1d8a8de83c, ; 887: lib_Xamarin.AndroidX.Lifecycle.Common.Jvm.dll.so => 268
	i64 u0xd12beacdfc14f696, ; 888: System.Dynamic.Runtime => 38
	i64 u0xd16fd7fb9bbcd43e, ; 889: Microsoft.Extensions.Diagnostics.Abstractions => 204
	i64 u0xd198e7ce1b6a8344, ; 890: System.Net.Quic.dll => 74
	i64 u0xd2505d8abeed6983, ; 891: lib_Microsoft.AspNetCore.Components.Web.dll.so => 184
	i64 u0xd3144156a3727ebe, ; 892: Xamarin.Google.Guava.ListenableFuture => 314
	i64 u0xd333d0af9e423810, ; 893: System.Runtime.InteropServices => 111
	i64 u0xd33a415cb4278969, ; 894: System.Security.Cryptography.Encoding.dll => 126
	i64 u0xd3426d966bb704f5, ; 895: Xamarin.AndroidX.AppCompat.AppCompatResources.dll => 241
	i64 u0xd3651b6fc3125825, ; 896: System.Private.Uri.dll => 90
	i64 u0xd373685349b1fe8b, ; 897: Microsoft.Extensions.Logging.dll => 213
	i64 u0xd3801faafafb7698, ; 898: System.Private.DataContractSerialization.dll => 89
	i64 u0xd3e4c8d6a2d5d470, ; 899: it/Microsoft.Maui.Controls.resources => 337
	i64 u0xd3edcc1f25459a50, ; 900: System.Reflection.Emit => 96
	i64 u0xd42655883bb8c19f, ; 901: Microsoft.EntityFrameworkCore.Abstractions.dll => 190
	i64 u0xd4645626dffec99d, ; 902: lib_Microsoft.Extensions.DependencyInjection.Abstractions.dll.so => 201
	i64 u0xd46b4a8758d1f3ee, ; 903: Microsoft.Extensions.FileProviders.Composite.dll => 206
	i64 u0xd4fa0abb79079ea9, ; 904: System.Security.Principal.dll => 132
	i64 u0xd5507e11a2b2839f, ; 905: Xamarin.AndroidX.Lifecycle.ViewModelSavedState => 280
	i64 u0xd5d04bef8478ea19, ; 906: Xamarin.AndroidX.Tracing.Tracing.dll => 299
	i64 u0xd60815f26a12e140, ; 907: Microsoft.Extensions.Logging.Debug.dll => 215
	i64 u0xd65786d27a4ad960, ; 908: lib_Microsoft.Maui.Controls.HotReload.Forms.dll.so => 360
	i64 u0xd6694f8359737e4e, ; 909: Xamarin.AndroidX.SavedState => 292
	i64 u0xd6949e129339eae5, ; 910: lib_Xamarin.AndroidX.Core.Core.Ktx.dll.so => 254
	i64 u0xd6d21782156bc35b, ; 911: Xamarin.AndroidX.SwipeRefreshLayout.dll => 298
	i64 u0xd6de019f6af72435, ; 912: Xamarin.AndroidX.ConstraintLayout.Core.dll => 251
	i64 u0xd70956d1e6deefb9, ; 913: Jsr305Binding => 311
	i64 u0xd72329819cbbbc44, ; 914: lib_Microsoft.Extensions.Configuration.Abstractions.dll.so => 196
	i64 u0xd72c760af136e863, ; 915: System.Xml.XmlSerializer.dll => 167
	i64 u0xd753f071e44c2a03, ; 916: lib_System.Security.SecureString.dll.so => 133
	i64 u0xd7b3764ada9d341d, ; 917: lib_Microsoft.Extensions.Logging.Abstractions.dll.so => 214
	i64 u0xd7f0088bc5ad71f2, ; 918: Xamarin.AndroidX.VersionedParcelable => 304
	i64 u0xd824ef6ab33f8f7a, ; 919: Xamarin.AndroidX.Window.WindowCore.dll => 308
	i64 u0xd8fb25e28ae30a12, ; 920: Xamarin.AndroidX.ProfileInstaller.ProfileInstaller.dll => 289
	i64 u0xda1dfa4c534a9251, ; 921: Microsoft.Extensions.DependencyInjection => 200
	i64 u0xdad05a11827959a3, ; 922: System.Collections.NonGeneric.dll => 11
	i64 u0xdaefdfe71aa53cf9, ; 923: System.IO.FileSystem.Primitives => 50
	i64 u0xdb5383ab5865c007, ; 924: lib-vi-Microsoft.Maui.Controls.resources.dll.so => 353
	i64 u0xdb58816721c02a59, ; 925: lib_System.Reflection.Emit.ILGeneration.dll.so => 94
	i64 u0xdbeda89f832aa805, ; 926: vi/Microsoft.Maui.Controls.resources.dll => 353
	i64 u0xdbf2a779fbc3ac31, ; 927: System.Transactions.Local.dll => 154
	i64 u0xdbf9607a441b4505, ; 928: System.Linq => 64
	i64 u0xdbfc90157a0de9b0, ; 929: lib_System.Text.Encoding.dll.so => 139
	i64 u0xdc75032002d1a212, ; 930: lib_System.Transactions.Local.dll.so => 154
	i64 u0xdca8be7403f92d4f, ; 931: lib_System.Linq.Queryable.dll.so => 63
	i64 u0xdce2c53525640bf3, ; 932: Microsoft.Extensions.Logging => 213
	i64 u0xdd2b722d78ef5f43, ; 933: System.Runtime.dll => 120
	i64 u0xdd67031857c72f96, ; 934: lib_System.Text.Encodings.Web.dll.so => 140
	i64 u0xdd70765ad6162057, ; 935: Xamarin.JSpecify => 316
	i64 u0xdd92e229ad292030, ; 936: System.Numerics.dll => 87
	i64 u0xdde30e6b77aa6f6c, ; 937: lib-zh-Hans-Microsoft.Maui.Controls.resources.dll.so => 355
	i64 u0xde01c9d672ab8d37, ; 938: lib_FokusApp.Presentation.dll.so => 359
	i64 u0xde110ae80fa7c2e2, ; 939: System.Xml.XDocument.dll => 163
	i64 u0xde4726fcdf63a198, ; 940: Xamarin.AndroidX.Transition => 301
	i64 u0xde572c2b2fb32f93, ; 941: lib_System.Threading.Tasks.Extensions.dll.so => 147
	i64 u0xde8769ebda7d8647, ; 942: hr/Microsoft.Maui.Controls.resources.dll => 334
	i64 u0xdee075f3477ef6be, ; 943: Xamarin.AndroidX.ExifInterface.dll => 263
	i64 u0xdf4b773de8fb1540, ; 944: System.Net.dll => 85
	i64 u0xdf9c7682560a9629, ; 945: System.Net.ServerSentEvents => 77
	i64 u0xdfa254ebb4346068, ; 946: System.Net.Ping => 72
	i64 u0xe0142572c095a480, ; 947: Xamarin.AndroidX.AppCompat.dll => 240
	i64 u0xe021eaa401792a05, ; 948: System.Text.Encoding.dll => 139
	i64 u0xe02f89350ec78051, ; 949: Xamarin.AndroidX.CoordinatorLayout.dll => 252
	i64 u0xe0496b9d65ef5474, ; 950: Xamarin.Android.Glide.DiskLruCache.dll => 233
	i64 u0xe10b760bb1462e7a, ; 951: lib_System.Security.Cryptography.Primitives.dll.so => 128
	i64 u0xe1566bbdb759c5af, ; 952: Microsoft.Maui.Controls.HotReload.Forms.dll => 360
	i64 u0xe192a588d4410686, ; 953: lib_System.IO.Pipelines.dll.so => 55
	i64 u0xe1a08bd3fa539e0d, ; 954: System.Runtime.Loader => 113
	i64 u0xe1a77eb8831f7741, ; 955: System.Security.SecureString.dll => 133
	i64 u0xe1b52f9f816c70ef, ; 956: System.Private.Xml.Linq.dll => 91
	i64 u0xe1e199c8ab02e356, ; 957: System.Data.DataSetExtensions.dll => 24
	i64 u0xe1ecfdb7fff86067, ; 958: System.Net.Security.dll => 76
	i64 u0xe2252a80fe853de4, ; 959: lib_System.Security.Principal.dll.so => 132
	i64 u0xe22fa4c9c645db62, ; 960: System.Diagnostics.TextWriterTraceListener.dll => 32
	i64 u0xe24095a7afddaab3, ; 961: lib_Microsoft.Extensions.Hosting.Abstractions.dll.so => 210
	i64 u0xe2420585aeceb728, ; 962: System.Net.Requests.dll => 75
	i64 u0xe26692647e6bcb62, ; 963: Xamarin.AndroidX.Lifecycle.Runtime.Ktx => 275
	i64 u0xe27532f50ce5b0b1, ; 964: Microsoft.Extensions.Localization.dll => 211
	i64 u0xe29b73bc11392966, ; 965: lib-id-Microsoft.Maui.Controls.resources.dll.so => 336
	i64 u0xe2ad448dee50fbdf, ; 966: System.Xml.Serialization => 162
	i64 u0xe2d920f978f5d85c, ; 967: System.Data.DataSetExtensions => 24
	i64 u0xe2e426c7714fa0bc, ; 968: Microsoft.Win32.Primitives.dll => 5
	i64 u0xe31089e70e4e84ee, ; 969: Microsoft.AspNetCore.Components.WebView.Maui => 186
	i64 u0xe332bacb3eb4a806, ; 970: Mono.Android.Export.dll => 174
	i64 u0xe3811d68d4fe8463, ; 971: pt-BR/Microsoft.Maui.Controls.resources.dll => 344
	i64 u0xe3b7cbae5ad66c75, ; 972: lib_System.Security.Cryptography.Encoding.dll.so => 126
	i64 u0xe4292b48f3224d5b, ; 973: lib_Xamarin.AndroidX.Core.ViewTree.dll.so => 255
	i64 u0xe494f7ced4ecd10a, ; 974: hu/Microsoft.Maui.Controls.resources.dll => 335
	i64 u0xe4a9b1e40d1e8917, ; 975: lib-fi-Microsoft.Maui.Controls.resources.dll.so => 330
	i64 u0xe4f74a0b5bf9703f, ; 976: System.Runtime.Serialization.Primitives => 117
	i64 u0xe5434e8a119ceb69, ; 977: lib_Mono.Android.dll.so => 176
	i64 u0xe55703b9ce5c038a, ; 978: System.Diagnostics.Tools => 33
	i64 u0xe57013c8afc270b5, ; 979: Microsoft.VisualBasic => 4
	i64 u0xe5b9c3bba59849e9, ; 980: FokusApp.Core.dll => 357
	i64 u0xe62913cc36bc07ec, ; 981: System.Xml.dll => 168
	i64 u0xe7bea09c4900a191, ; 982: Xamarin.AndroidX.VectorDrawable.dll => 302
	i64 u0xe7e03cc18dcdeb49, ; 983: lib_System.Diagnostics.StackTrace.dll.so => 31
	i64 u0xe7e147ff99a7a380, ; 984: lib_System.Configuration.dll.so => 20
	i64 u0xe8397cf3948e7cb7, ; 985: lib_Microsoft.Extensions.Options.ConfigurationExtensions.dll.so => 217
	i64 u0xe86b0df4ba9e5db8, ; 986: lib_Xamarin.AndroidX.Lifecycle.Runtime.Android.dll.so => 274
	i64 u0xe896622fe0902957, ; 987: System.Reflection.Emit.dll => 96
	i64 u0xe89a2a9ef110899b, ; 988: System.Drawing.dll => 37
	i64 u0xe8c5f8c100b5934b, ; 989: Microsoft.Win32.Registry => 6
	i64 u0xe9772100456fb4b4, ; 990: Microsoft.AspNetCore.Components.dll => 182
	i64 u0xe98163eb702ae5c5, ; 991: Xamarin.AndroidX.Arch.Core.Runtime => 243
	i64 u0xe994f23ba4c143e5, ; 992: Xamarin.KotlinX.Coroutines.Android => 318
	i64 u0xe9b9c8c0458fd92a, ; 993: System.Windows => 159
	i64 u0xe9d166d87a7f2bdb, ; 994: lib_Xamarin.AndroidX.Startup.StartupRuntime.dll.so => 297
	i64 u0xea154e342c6ac70f, ; 995: Microsoft.Extensions.FileProviders.Embedded.dll => 207
	i64 u0xea5a4efc2ad81d1b, ; 996: Xamarin.Google.ErrorProne.Annotations => 313
	i64 u0xeaa4fe436a4db9bb, ; 997: FokusApp.Core => 357
	i64 u0xeb2313fe9d65b785, ; 998: Xamarin.AndroidX.ConstraintLayout.dll => 250
	i64 u0xec8abb68d340aac6, ; 999: Microsoft.AspNetCore.Authorization => 181
	i64 u0xed19c616b3fcb7eb, ; 1000: Xamarin.AndroidX.VersionedParcelable.dll => 304
	i64 u0xed60c6fa891c051a, ; 1001: lib_Microsoft.VisualStudio.DesignTools.TapContract.dll.so => 362
	i64 u0xedc4817167106c23, ; 1002: System.Net.Sockets.dll => 79
	i64 u0xedc632067fb20ff3, ; 1003: System.Memory.dll => 65
	i64 u0xedc8e4ca71a02a8b, ; 1004: Xamarin.AndroidX.Navigation.Runtime.dll => 286
	i64 u0xee25c2570ce19192, ; 1005: lib_Microsoft.Extensions.Localization.Abstractions.dll.so => 212
	i64 u0xee81f5b3f1c4f83b, ; 1006: System.Threading.ThreadPool => 151
	i64 u0xeeb7ebb80150501b, ; 1007: lib_Xamarin.AndroidX.Collection.Jvm.dll.so => 247
	i64 u0xeefc635595ef57f0, ; 1008: System.Security.Cryptography.Cng => 124
	i64 u0xef03b1b5a04e9709, ; 1009: System.Text.Encoding.CodePages.dll => 137
	i64 u0xef5bcbe61622ee5f, ; 1010: Xamarin.AndroidX.Tracing.Tracing.Android.dll => 300
	i64 u0xef602c523fe2e87a, ; 1011: lib_Xamarin.Google.Guava.ListenableFuture.dll.so => 314
	i64 u0xef72742e1bcca27a, ; 1012: Microsoft.Maui.Essentials.dll => 224
	i64 u0xefd1e0c4e5c9b371, ; 1013: System.Resources.ResourceManager.dll => 103
	i64 u0xefe8f8d5ed3c72ea, ; 1014: System.Formats.Tar.dll => 40
	i64 u0xefec0b7fdc57ec42, ; 1015: Xamarin.AndroidX.Activity => 235
	i64 u0xeff59cbde4363ec3, ; 1016: System.Threading.AccessControl.dll => 143
	i64 u0xf00c29406ea45e19, ; 1017: es/Microsoft.Maui.Controls.resources.dll => 329
	i64 u0xf09e47b6ae914f6e, ; 1018: System.Net.NameResolution => 70
	i64 u0xf0ac2b489fed2e35, ; 1019: lib_System.Diagnostics.Debug.dll.so => 27
	i64 u0xf0bb49dadd3a1fe1, ; 1020: lib_System.Net.ServicePoint.dll.so => 78
	i64 u0xf0c16dff90fbf5d6, ; 1021: Xamarin.AndroidX.Window.WindowCore.Jvm => 309
	i64 u0xf0de2537ee19c6ca, ; 1022: lib_System.Net.WebHeaderCollection.dll.so => 81
	i64 u0xf1138779fa181c68, ; 1023: lib_Xamarin.AndroidX.Lifecycle.Runtime.dll.so => 273
	i64 u0xf11b621fc87b983f, ; 1024: Microsoft.Maui.Controls.Xaml.dll => 222
	i64 u0xf161f4f3c3b7e62c, ; 1025: System.Data => 25
	i64 u0xf16eb650d5a464bc, ; 1026: System.ValueTuple => 156
	i64 u0xf1c4b4005493d871, ; 1027: System.Formats.Asn1.dll => 39
	i64 u0xf22514cfad2d598b, ; 1028: lib_Xamarin.AndroidX.Lifecycle.ViewModelSavedState.Android.dll.so => 281
	i64 u0xf238bd79489d3a96, ; 1029: lib-nl-Microsoft.Maui.Controls.resources.dll.so => 342
	i64 u0xf2feea356ba760af, ; 1030: Xamarin.AndroidX.Arch.Core.Runtime.dll => 243
	i64 u0xf300e085f8acd238, ; 1031: lib_System.ServiceProcess.dll.so => 136
	i64 u0xf34e52b26e7e059d, ; 1032: System.Runtime.CompilerServices.VisualC.dll => 106
	i64 u0xf37221fda4ef8830, ; 1033: lib_Xamarin.Google.Android.Material.dll.so => 310
	i64 u0xf3ad9b8fb3eefd12, ; 1034: lib_System.IO.UnmanagedMemoryStream.dll.so => 58
	i64 u0xf3ddfe05336abf29, ; 1035: System => 169
	i64 u0xf408654b2a135055, ; 1036: System.Reflection.Emit.ILGeneration.dll => 94
	i64 u0xf4103170a1de5bd0, ; 1037: System.Linq.Queryable.dll => 63
	i64 u0xf42d20c23173d77c, ; 1038: lib_System.ServiceModel.Web.dll.so => 135
	i64 u0xf437b94630896f3f, ; 1039: lib_Plugin.Maui.Audio.dll.so => 226
	i64 u0xf4c1dd70a5496a17, ; 1040: System.IO.Compression => 47
	i64 u0xf4ecf4b9afc64781, ; 1041: System.ServiceProcess.dll => 136
	i64 u0xf4eeeaa566e9b970, ; 1042: lib_Xamarin.AndroidX.CustomView.PoolingContainer.dll.so => 258
	i64 u0xf518f63ead11fcd1, ; 1043: System.Threading.Tasks => 149
	i64 u0xf5fc7602fe27b333, ; 1044: System.Net.WebHeaderCollection => 81
	i64 u0xf6077741019d7428, ; 1045: Xamarin.AndroidX.CoordinatorLayout => 252
	i64 u0xf6742cbf457c450b, ; 1046: Xamarin.AndroidX.Lifecycle.Runtime.Android.dll => 274
	i64 u0xf6de7fa3776f8927, ; 1047: lib_Microsoft.Extensions.Configuration.Json.dll.so => 199
	i64 u0xf6e8de2aebcbb422, ; 1048: lib_Xamarin.AndroidX.Window.WindowCore.Jvm.dll.so => 309
	i64 u0xf6f893f692f8cb43, ; 1049: Microsoft.Extensions.Options.ConfigurationExtensions.dll => 217
	i64 u0xf70c0a7bf8ccf5af, ; 1050: System.Web => 158
	i64 u0xf77b20923f07c667, ; 1051: de/Microsoft.Maui.Controls.resources.dll => 327
	i64 u0xf7e2cac4c45067b3, ; 1052: lib_System.Numerics.Vectors.dll.so => 86
	i64 u0xf7e74930e0e3d214, ; 1053: zh-HK/Microsoft.Maui.Controls.resources.dll => 354
	i64 u0xf84773b5c81e3cef, ; 1054: lib-uk-Microsoft.Maui.Controls.resources.dll.so => 352
	i64 u0xf8aac5ea82de1348, ; 1055: System.Linq.Queryable => 63
	i64 u0xf8b77539b362d3ba, ; 1056: lib_System.Reflection.Primitives.dll.so => 99
	i64 u0xf8e045dc345b2ea3, ; 1057: lib_Xamarin.AndroidX.RecyclerView.dll.so => 290
	i64 u0xf915dc29808193a1, ; 1058: System.Web.HttpUtility.dll => 157
	i64 u0xf91db4f1ad5d43b1, ; 1059: Plugin.Maui.Audio.dll => 226
	i64 u0xf96c777a2a0686f4, ; 1060: hi/Microsoft.Maui.Controls.resources.dll => 333
	i64 u0xf9be54c8bcf8ff3b, ; 1061: System.Security.AccessControl.dll => 121
	i64 u0xf9eec5bb3a6aedc6, ; 1062: Microsoft.Extensions.Options => 216
	i64 u0xfa0e82300e67f913, ; 1063: lib_System.AppContext.dll.so => 7
	i64 u0xfa2fdb27e8a2c8e8, ; 1064: System.ComponentModel.EventBasedAsync => 16
	i64 u0xfa3f278f288b0e84, ; 1065: lib_System.Net.Security.dll.so => 76
	i64 u0xfa504dfa0f097d72, ; 1066: Microsoft.Extensions.FileProviders.Abstractions.dll => 205
	i64 u0xfa5ed7226d978949, ; 1067: lib-ar-Microsoft.Maui.Controls.resources.dll.so => 323
	i64 u0xfa645d91e9fc4cba, ; 1068: System.Threading.Thread => 150
	i64 u0xfad4d2c770e827f9, ; 1069: lib_System.IO.IsolatedStorage.dll.so => 53
	i64 u0xfb022853d73b7fa5, ; 1070: lib_SQLitePCLRaw.batteries_v2.dll.so => 227
	i64 u0xfb06dd2338e6f7c4, ; 1071: System.Net.Ping.dll => 72
	i64 u0xfb087abe5365e3b7, ; 1072: lib_System.Data.DataSetExtensions.dll.so => 24
	i64 u0xfb846e949baff5ea, ; 1073: System.Xml.Serialization.dll => 162
	i64 u0xfbad3e4ce4b98145, ; 1074: System.Security.Cryptography.X509Certificates => 129
	i64 u0xfbf0a31c9fc34bc4, ; 1075: lib_System.Net.Http.dll.so => 67
	i64 u0xfc6b7527cc280b3f, ; 1076: lib_System.Runtime.Serialization.Formatters.dll.so => 115
	i64 u0xfc719aec26adf9d9, ; 1077: Xamarin.AndroidX.Navigation.Fragment.dll => 285
	i64 u0xfc82690c2fe2735c, ; 1078: Xamarin.AndroidX.Lifecycle.Process.dll => 272
	i64 u0xfc93fc307d279893, ; 1079: System.IO.Pipes.AccessControl.dll => 56
	i64 u0xfcd302092ada6328, ; 1080: System.IO.MemoryMappedFiles.dll => 54
	i64 u0xfd22f00870e40ae0, ; 1081: lib_Xamarin.AndroidX.DrawerLayout.dll.so => 259
	i64 u0xfd2e866c678cac90, ; 1082: lib_Microsoft.AspNetCore.Components.WebView.Maui.dll.so => 186
	i64 u0xfd49b3c1a76e2748, ; 1083: System.Runtime.InteropServices.RuntimeInformation => 110
	i64 u0xfd536c702f64dc47, ; 1084: System.Text.Encoding.Extensions => 138
	i64 u0xfd583f7657b6a1cb, ; 1085: Xamarin.AndroidX.Fragment => 264
	i64 u0xfd8dd91a2c26bd5d, ; 1086: Xamarin.AndroidX.Lifecycle.Runtime => 273
	i64 u0xfda36abccf05cf5c, ; 1087: System.Net.WebSockets.Client => 83
	i64 u0xfddbe9695626a7f5, ; 1088: Xamarin.AndroidX.Lifecycle.Common => 267
	i64 u0xfe9856c3af9365ab, ; 1089: lib_Microsoft.Extensions.Configuration.FileExtensions.dll.so => 198
	i64 u0xfeae9952cf03b8cb, ; 1090: tr/Microsoft.Maui.Controls.resources => 351
	i64 u0xfebe1950717515f9, ; 1091: Xamarin.AndroidX.Lifecycle.LiveData.Core.Ktx.dll => 271
	i64 u0xff270a55858bac8d, ; 1092: System.Security.Principal => 132
	i64 u0xff9b54613e0d2cc8, ; 1093: System.Net.Http.Json => 66
	i64 u0xffdb7a971be4ec73 ; 1094: System.ValueTuple.dll => 156
], align 16

@assembly_image_cache_indices = dso_local local_unnamed_addr constant [1095 x i32] [
	i32 43, i32 319, i32 298, i32 14, i32 286, i32 217, i32 108, i32 194,
	i32 175, i32 49, i32 240, i32 8, i32 230, i32 89, i32 347, i32 325,
	i32 353, i32 260, i32 73, i32 290, i32 188, i32 13, i32 223, i32 105,
	i32 188, i32 354, i32 160, i32 20, i32 265, i32 247, i32 182, i32 165,
	i32 262, i32 302, i32 171, i32 347, i32 11, i32 215, i32 303, i32 99,
	i32 258, i32 0, i32 259, i32 14, i32 216, i32 11, i32 130, i32 98,
	i32 193, i32 144, i32 40, i32 348, i32 322, i32 305, i32 344, i32 176,
	i32 234, i32 6, i32 224, i32 69, i32 295, i32 133, i32 294, i32 261,
	i32 70, i32 248, i32 68, i32 58, i32 257, i32 53, i32 358, i32 44,
	i32 128, i32 69, i32 84, i32 275, i32 362, i32 162, i32 95, i32 102,
	i32 290, i32 145, i32 155, i32 244, i32 331, i32 166, i32 173, i32 332,
	i32 201, i32 84, i32 362, i32 316, i32 248, i32 5, i32 6, i32 52,
	i32 104, i32 202, i32 57, i32 123, i32 101, i32 172, i32 121, i32 319,
	i32 22, i32 335, i32 140, i32 100, i32 322, i32 80, i32 341, i32 297,
	i32 122, i32 9, i32 169, i32 350, i32 72, i32 233, i32 183, i32 276,
	i32 291, i32 203, i32 208, i32 175, i32 149, i32 41, i32 295, i32 48,
	i32 31, i32 288, i32 339, i32 148, i32 216, i32 167, i32 29, i32 87,
	i32 299, i32 80, i32 44, i32 30, i32 219, i32 43, i32 106, i32 120,
	i32 238, i32 46, i32 94, i32 350, i32 57, i32 152, i32 361, i32 358,
	i32 150, i32 189, i32 103, i32 50, i32 21, i32 253, i32 117, i32 231,
	i32 331, i32 312, i32 227, i32 317, i32 218, i32 97, i32 59, i32 336,
	i32 334, i32 219, i32 308, i32 84, i32 312, i32 173, i32 27, i32 73,
	i32 289, i32 204, i32 263, i32 284, i32 360, i32 352, i32 71, i32 34,
	i32 330, i32 15, i32 142, i32 39, i32 356, i32 209, i32 249, i32 343,
	i32 137, i32 95, i32 91, i32 153, i32 309, i32 349, i32 25, i32 141,
	i32 58, i32 143, i32 52, i32 328, i32 220, i32 30, i32 161, i32 35,
	i32 168, i32 193, i32 264, i32 53, i32 207, i32 364, i32 307, i32 93,
	i32 245, i32 36, i32 331, i32 161, i32 209, i32 10, i32 329, i32 79,
	i32 60, i32 56, i32 206, i32 223, i32 325, i32 221, i32 14, i32 306,
	i32 195, i32 242, i32 112, i32 279, i32 33, i32 107, i32 87, i32 95,
	i32 54, i32 99, i32 315, i32 59, i32 10, i32 105, i32 257, i32 70,
	i32 305, i32 324, i32 205, i32 128, i32 291, i32 119, i32 138, i32 220,
	i32 293, i32 129, i32 109, i32 317, i32 134, i32 244, i32 358, i32 314,
	i32 151, i32 160, i32 265, i32 253, i32 227, i32 198, i32 260, i32 291,
	i32 100, i32 25, i32 185, i32 296, i32 147, i32 210, i32 283, i32 4,
	i32 171, i32 241, i32 211, i32 103, i32 165, i32 102, i32 255, i32 26,
	i32 96, i32 172, i32 176, i32 236, i32 4, i32 343, i32 1, i32 300,
	i32 262, i32 2, i32 117, i32 317, i32 190, i32 265, i32 272, i32 34,
	i32 7, i32 202, i32 347, i32 160, i32 345, i32 54, i32 88, i32 212,
	i32 304, i32 288, i32 45, i32 271, i32 107, i32 48, i32 141, i32 281,
	i32 66, i32 191, i32 282, i32 71, i32 83, i32 61, i32 92, i32 158,
	i32 242, i32 136, i32 113, i32 60, i32 337, i32 282, i32 220, i32 289,
	i32 175, i32 137, i32 144, i32 41, i32 324, i32 229, i32 197, i32 221,
	i32 359, i32 62, i32 179, i32 197, i32 278, i32 82, i32 26, i32 37,
	i32 102, i32 275, i32 73, i32 23, i32 253, i32 225, i32 348, i32 124,
	i32 71, i32 110, i32 354, i32 122, i32 120, i32 267, i32 284, i32 268,
	i32 12, i32 3, i32 127, i32 118, i32 146, i32 42, i32 90, i32 237,
	i32 228, i32 178, i32 210, i32 28, i32 152, i32 197, i32 338, i32 200,
	i32 313, i32 236, i32 1, i32 2, i32 238, i32 45, i32 252, i32 153,
	i32 19, i32 89, i32 326, i32 42, i32 271, i32 246, i32 276, i32 97,
	i32 213, i32 29, i32 42, i32 81, i32 186, i32 261, i32 249, i32 148,
	i32 111, i32 247, i32 12, i32 108, i32 140, i32 17, i32 125, i32 68,
	i32 161, i32 23, i32 229, i32 328, i32 321, i32 105, i32 200, i32 320,
	i32 65, i32 59, i32 222, i32 327, i32 113, i32 178, i32 287, i32 363,
	i32 318, i32 10, i32 310, i32 123, i32 101, i32 108, i32 77, i32 279,
	i32 221, i32 114, i32 239, i32 50, i32 60, i32 21, i32 278, i32 256,
	i32 74, i32 251, i32 159, i32 40, i32 326, i32 187, i32 36, i32 39,
	i32 332, i32 229, i32 111, i32 341, i32 22, i32 315, i32 211, i32 277,
	i32 225, i32 16, i32 218, i32 82, i32 82, i32 256, i32 218, i32 285,
	i32 294, i32 156, i32 22, i32 223, i32 325, i32 51, i32 52, i32 219,
	i32 351, i32 341, i32 97, i32 232, i32 204, i32 337, i32 17, i32 255,
	i32 126, i32 334, i32 164, i32 46, i32 313, i32 180, i32 119, i32 65,
	i32 192, i32 170, i32 203, i32 195, i32 15, i32 292, i32 114, i32 239,
	i32 62, i32 199, i32 77, i32 124, i32 340, i32 3, i32 350, i32 264,
	i32 277, i32 316, i32 185, i32 277, i32 7, i32 246, i32 330, i32 260,
	i32 188, i32 18, i32 348, i32 327, i32 80, i32 250, i32 187, i32 284,
	i32 134, i32 315, i32 340, i32 183, i32 86, i32 215, i32 13, i32 35,
	i32 122, i32 321, i32 272, i32 177, i32 262, i32 88, i32 231, i32 19,
	i32 305, i32 196, i32 270, i32 74, i32 226, i32 361, i32 98, i32 230,
	i32 184, i32 169, i32 266, i32 212, i32 85, i32 0, i32 356, i32 240,
	i32 245, i32 158, i32 37, i32 155, i32 352, i32 355, i32 206, i32 148,
	i32 57, i32 116, i32 191, i32 246, i32 302, i32 359, i32 301, i32 38,
	i32 356, i32 195, i32 118, i32 209, i32 238, i32 15, i32 232, i32 190,
	i32 150, i32 44, i32 224, i32 236, i32 101, i32 320, i32 172, i32 17,
	i32 293, i32 49, i32 110, i32 100, i32 193, i32 282, i32 28, i32 131,
	i32 30, i32 332, i32 205, i32 294, i32 131, i32 45, i32 256, i32 192,
	i32 261, i32 153, i32 9, i32 300, i32 283, i32 333, i32 346, i32 228,
	i32 345, i32 135, i32 344, i32 43, i32 321, i32 228, i32 34, i32 364,
	i32 47, i32 147, i32 278, i32 222, i32 269, i32 257, i32 199, i32 141,
	i32 64, i32 135, i32 324, i32 49, i32 164, i32 243, i32 269, i32 232,
	i32 267, i32 340, i32 301, i32 47, i32 168, i32 177, i32 266, i32 0,
	i32 329, i32 263, i32 336, i32 225, i32 19, i32 9, i32 180, i32 254,
	i32 293, i32 127, i32 61, i32 145, i32 179, i32 286, i32 339, i32 273,
	i32 311, i32 307, i32 154, i32 146, i32 319, i32 129, i32 318, i32 164,
	i32 166, i32 258, i32 357, i32 235, i32 196, i32 287, i32 342, i32 27,
	i32 283, i32 270, i32 208, i32 85, i32 307, i32 130, i32 312, i32 104,
	i32 152, i32 310, i32 308, i32 288, i32 55, i32 166, i32 171, i32 134,
	i32 207, i32 38, i32 303, i32 339, i32 203, i32 179, i32 183, i32 23,
	i32 115, i32 93, i32 51, i32 62, i32 125, i32 86, i32 130, i32 167,
	i32 311, i32 170, i32 292, i32 295, i32 259, i32 231, i32 274, i32 5,
	i32 268, i32 335, i32 174, i32 3, i32 208, i32 279, i32 119, i32 363,
	i32 237, i32 20, i32 214, i32 92, i32 67, i32 31, i32 201, i32 328,
	i32 251, i32 61, i32 114, i32 270, i32 33, i32 131, i32 163, i32 346,
	i32 249, i32 144, i32 342, i32 157, i32 18, i32 248, i32 234, i32 78,
	i32 76, i32 16, i32 173, i32 88, i32 192, i32 127, i32 269, i32 280,
	i32 250, i32 349, i32 276, i32 35, i32 182, i32 121, i32 142, i32 125,
	i32 109, i32 326, i32 361, i32 303, i32 245, i32 333, i32 323, i32 55,
	i32 48, i32 29, i32 143, i32 149, i32 214, i32 151, i32 36, i32 349,
	i32 178, i32 78, i32 165, i32 2, i32 281, i32 296, i32 345, i32 338,
	i32 163, i32 13, i32 159, i32 185, i32 155, i32 79, i32 106, i32 115,
	i32 230, i32 242, i32 187, i32 181, i32 67, i32 68, i32 306, i32 46,
	i32 244, i32 112, i32 8, i32 241, i32 56, i32 237, i32 66, i32 323,
	i32 254, i32 21, i32 112, i32 104, i32 64, i32 146, i32 189, i32 181,
	i32 235, i32 8, i32 338, i32 174, i32 51, i32 306, i32 118, i32 202,
	i32 145, i32 180, i32 170, i32 1, i32 83, i32 116, i32 189, i32 280,
	i32 194, i32 18, i32 75, i32 285, i32 92, i32 233, i32 90, i32 123,
	i32 299, i32 198, i32 239, i32 287, i32 138, i32 157, i32 109, i32 12,
	i32 93, i32 32, i32 184, i32 194, i32 351, i32 139, i32 343, i32 346,
	i32 297, i32 191, i32 234, i32 41, i32 364, i32 296, i32 142, i32 320,
	i32 322, i32 26, i32 177, i32 355, i32 75, i32 266, i32 298, i32 363,
	i32 28, i32 69, i32 91, i32 98, i32 116, i32 32, i32 107, i32 268,
	i32 38, i32 204, i32 74, i32 184, i32 314, i32 111, i32 126, i32 241,
	i32 90, i32 213, i32 89, i32 337, i32 96, i32 190, i32 201, i32 206,
	i32 132, i32 280, i32 299, i32 215, i32 360, i32 292, i32 254, i32 298,
	i32 251, i32 311, i32 196, i32 167, i32 133, i32 214, i32 304, i32 308,
	i32 289, i32 200, i32 11, i32 50, i32 353, i32 94, i32 353, i32 154,
	i32 64, i32 139, i32 154, i32 63, i32 213, i32 120, i32 140, i32 316,
	i32 87, i32 355, i32 359, i32 163, i32 301, i32 147, i32 334, i32 263,
	i32 85, i32 77, i32 72, i32 240, i32 139, i32 252, i32 233, i32 128,
	i32 360, i32 55, i32 113, i32 133, i32 91, i32 24, i32 76, i32 132,
	i32 32, i32 210, i32 75, i32 275, i32 211, i32 336, i32 162, i32 24,
	i32 5, i32 186, i32 174, i32 344, i32 126, i32 255, i32 335, i32 330,
	i32 117, i32 176, i32 33, i32 4, i32 357, i32 168, i32 302, i32 31,
	i32 20, i32 217, i32 274, i32 96, i32 37, i32 6, i32 182, i32 243,
	i32 318, i32 159, i32 297, i32 207, i32 313, i32 357, i32 250, i32 181,
	i32 304, i32 362, i32 79, i32 65, i32 286, i32 212, i32 151, i32 247,
	i32 124, i32 137, i32 300, i32 314, i32 224, i32 103, i32 40, i32 235,
	i32 143, i32 329, i32 70, i32 27, i32 78, i32 309, i32 81, i32 273,
	i32 222, i32 25, i32 156, i32 39, i32 281, i32 342, i32 243, i32 136,
	i32 106, i32 310, i32 58, i32 169, i32 94, i32 63, i32 135, i32 226,
	i32 47, i32 136, i32 258, i32 149, i32 81, i32 252, i32 274, i32 199,
	i32 309, i32 217, i32 158, i32 327, i32 86, i32 354, i32 352, i32 63,
	i32 99, i32 290, i32 157, i32 226, i32 333, i32 121, i32 216, i32 7,
	i32 16, i32 76, i32 205, i32 323, i32 150, i32 53, i32 227, i32 72,
	i32 24, i32 162, i32 129, i32 67, i32 115, i32 285, i32 272, i32 56,
	i32 54, i32 259, i32 186, i32 110, i32 138, i32 264, i32 273, i32 83,
	i32 267, i32 198, i32 351, i32 271, i32 132, i32 66, i32 156
], align 16

@marshal_methods_number_of_classes = dso_local local_unnamed_addr constant i32 0, align 4

@marshal_methods_class_cache = dso_local local_unnamed_addr global [0 x %struct.MarshalMethodsManagedClass] zeroinitializer, align 8

; Names of classes in which marshal methods reside
@mm_class_names = dso_local local_unnamed_addr constant [0 x ptr] zeroinitializer, align 8

@mm_method_names = dso_local local_unnamed_addr constant [1 x %struct.MarshalMethodName] [
	%struct.MarshalMethodName {
		i64 u0x0000000000000000, ; name: 
		ptr @.MarshalMethodName.0_name; char* name
	} ; 0
], align 8

; get_function_pointer (uint32_t mono_image_index, uint32_t class_index, uint32_t method_token, void*& target_ptr)
@get_function_pointer = internal dso_local unnamed_addr global ptr null, align 8

; Functions

; Function attributes: memory(write, argmem: none, inaccessiblemem: none) "min-legal-vector-width"="0" mustprogress nofree norecurse nosync "no-trapping-math"="true" nounwind "stack-protector-buffer-size"="8" uwtable willreturn
define void @xamarin_app_init(ptr nocapture noundef readnone %env, ptr noundef %fn) local_unnamed_addr #0
{
	%fnIsNull = icmp eq ptr %fn, null
	br i1 %fnIsNull, label %1, label %2

1: ; preds = %0
	%putsResult = call noundef i32 @puts(ptr @.mm.0)
	call void @abort()
	unreachable 

2: ; preds = %1, %0
	store ptr %fn, ptr @get_function_pointer, align 8, !tbaa !3
	ret void
}

; Strings
@.mm.0 = private unnamed_addr constant [40 x i8] c"get_function_pointer MUST be specified\0A\00", align 16

;MarshalMethodName
@.MarshalMethodName.0_name = private unnamed_addr constant [1 x i8] c"\00", align 1

; External functions

; Function attributes: noreturn "no-trapping-math"="true" nounwind "stack-protector-buffer-size"="8"
declare void @abort() local_unnamed_addr #2

; Function attributes: nofree nounwind
declare noundef i32 @puts(ptr noundef) local_unnamed_addr #1
attributes #0 = { memory(write, argmem: none, inaccessiblemem: none) "min-legal-vector-width"="0" mustprogress nofree norecurse nosync "no-trapping-math"="true" nounwind "stack-protector-buffer-size"="8" "target-cpu"="x86-64" "target-features"="+crc32,+cx16,+cx8,+fxsr,+mmx,+popcnt,+sse,+sse2,+sse3,+sse4.1,+sse4.2,+ssse3,+x87" "tune-cpu"="generic" uwtable willreturn }
attributes #1 = { nofree nounwind }
attributes #2 = { noreturn "no-trapping-math"="true" nounwind "stack-protector-buffer-size"="8" "target-cpu"="x86-64" "target-features"="+crc32,+cx16,+cx8,+fxsr,+mmx,+popcnt,+sse,+sse2,+sse3,+sse4.1,+sse4.2,+ssse3,+x87" "tune-cpu"="generic" }

; Metadata
!llvm.module.flags = !{!0, !1}
!0 = !{i32 1, !"wchar_size", i32 4}
!1 = !{i32 7, !"PIC Level", i32 2}
!llvm.ident = !{!2}
!2 = !{!".NET for Android remotes/origin/release/10.0.1xx @ d549e1dc4e2a083b08b4f24cb5495e81b99d79b5"}
!3 = !{!4, !4, i64 0}
!4 = !{!"any pointer", !5, i64 0}
!5 = !{!"omnipotent char", !6, i64 0}
!6 = !{!"Simple C++ TBAA"}
