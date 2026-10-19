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

@assembly_image_cache = dso_local local_unnamed_addr global [164 x ptr] zeroinitializer, align 16

; Each entry maps hash of an assembly name to an index into the `assembly_image_cache` array
@assembly_image_cache_hashes = dso_local local_unnamed_addr constant [492 x i64] [
	i64 u0x0071cf2d27b7d61e, ; 0: lib_Xamarin.AndroidX.SwipeRefreshLayout.dll.so => 98
	i64 u0x01109b0e4d99e61f, ; 1: System.ComponentModel.Annotations.dll => 115
	i64 u0x02a4c5a44384f885, ; 2: Microsoft.Extensions.Caching.Memory => 47
	i64 u0x02abedc11addc1ed, ; 3: lib_Mono.Android.Runtime.dll.so => 162
	i64 u0x032267b2a94db371, ; 4: lib_Xamarin.AndroidX.AppCompat.dll.so => 77
	i64 u0x0363ac97a4cb84e6, ; 5: SQLitePCLRaw.provider.e_sqlite3.dll => 75
	i64 u0x043032f1d071fae0, ; 6: ru/Microsoft.Maui.Controls.resources => 25
	i64 u0x044440a55165631e, ; 7: lib-cs-Microsoft.Maui.Controls.resources.dll.so => 3
	i64 u0x046eb1581a80c6b0, ; 8: vi/Microsoft.Maui.Controls.resources => 31
	i64 u0x0517ef04e06e9f76, ; 9: System.Net.Primitives => 137
	i64 u0x0565d18c6da3de38, ; 10: Xamarin.AndroidX.RecyclerView => 96
	i64 u0x057bf9fa9fb09f7c, ; 11: Microsoft.Data.Sqlite.dll => 41
	i64 u0x0581db89237110e9, ; 12: lib_System.Collections.dll.so => 114
	i64 u0x05989cb940b225a9, ; 13: Microsoft.Maui.dll => 68
	i64 u0x05ef98b6a1db882c, ; 14: lib_Microsoft.Data.Sqlite.dll.so => 41
	i64 u0x06076b5d2b581f08, ; 15: zh-HK/Microsoft.Maui.Controls.resources => 32
	i64 u0x0680a433c781bb3d, ; 16: Xamarin.AndroidX.Collection.Jvm => 80
	i64 u0x0690533f9fc14683, ; 17: lib_Microsoft.AspNetCore.Components.dll.so => 37
	i64 u0x07c57877c7ba78ad, ; 18: ru/Microsoft.Maui.Controls.resources.dll => 25
	i64 u0x07dcdc7460a0c5e4, ; 19: System.Collections.NonGeneric => 112
	i64 u0x08da3c7dbc340741, ; 20: en/FokusApp.resources.dll => 0
	i64 u0x08f3c9788ee2153c, ; 21: Xamarin.AndroidX.DrawerLayout => 85
	i64 u0x09138715c92dba90, ; 22: lib_System.ComponentModel.Annotations.dll.so => 115
	i64 u0x0919c28b89381a0b, ; 23: lib_Microsoft.Extensions.Options.dll.so => 63
	i64 u0x092266563089ae3e, ; 24: lib_System.Collections.NonGeneric.dll.so => 112
	i64 u0x09d144a7e214d457, ; 25: System.Security.Cryptography => 149
	i64 u0x0a805f95d98f597b, ; 26: lib_Microsoft.Extensions.Caching.Abstractions.dll.so => 46
	i64 u0x0b3b632c3bbee20c, ; 27: sk/Microsoft.Maui.Controls.resources => 26
	i64 u0x0b6aff547b84fbe9, ; 28: Xamarin.KotlinX.Serialization.Core.Jvm => 105
	i64 u0x0be2e1f8ce4064ed, ; 29: Xamarin.AndroidX.ViewPager => 100
	i64 u0x0c3ca6cc978e2aae, ; 30: pt-BR/Microsoft.Maui.Controls.resources => 22
	i64 u0x0c59ad9fbbd43abe, ; 31: Mono.Android => 163
	i64 u0x0c7790f60165fc06, ; 32: lib_Microsoft.Maui.Essentials.dll.so => 69
	i64 u0x102a31b45304b1da, ; 33: Xamarin.AndroidX.CustomView => 84
	i64 u0x10a6fa3e9d5ee60e, ; 34: FokusApp.Migrations.dll => 107
	i64 u0x10f6cfcbcf801616, ; 35: System.IO.Compression.Brotli => 127
	i64 u0x125b7f94acb989db, ; 36: Xamarin.AndroidX.RecyclerView.dll => 96
	i64 u0x13a01de0cbc3f06c, ; 37: lib-fr-Microsoft.Maui.Controls.resources.dll.so => 9
	i64 u0x13f1e5e209e91af4, ; 38: lib_Java.Interop.dll.so => 160
	i64 u0x13f1e880c25d96d1, ; 39: he/Microsoft.Maui.Controls.resources => 10
	i64 u0x143d8ea60a6a4011, ; 40: Microsoft.Extensions.DependencyInjection.Abstractions => 51
	i64 u0x16054fdcb6b3098b, ; 41: Microsoft.Extensions.DependencyModel.dll => 52
	i64 u0x17125c9a85b4929f, ; 42: lib_netstandard.dll.so => 158
	i64 u0x17b56e25558a5d36, ; 43: lib-hu-Microsoft.Maui.Controls.resources.dll.so => 13
	i64 u0x17f9358913beb16a, ; 44: System.Text.Encodings.Web => 150
	i64 u0x18402a709e357f3b, ; 45: lib_Xamarin.KotlinX.Serialization.Core.Jvm.dll.so => 105
	i64 u0x18f0ce884e87d89a, ; 46: nb/Microsoft.Maui.Controls.resources.dll => 19
	i64 u0x1a91866a319e9259, ; 47: lib_System.Collections.Concurrent.dll.so => 110
	i64 u0x1aac34d1917ba5d3, ; 48: lib_System.dll.so => 157
	i64 u0x1aad60783ffa3e5b, ; 49: lib-th-Microsoft.Maui.Controls.resources.dll.so => 28
	i64 u0x1c5217a9e4973753, ; 50: lib_Microsoft.Extensions.FileProviders.Physical.dll.so => 56
	i64 u0x1c753b5ff15bce1b, ; 51: Mono.Android.Runtime.dll => 162
	i64 u0x1e3d87657e9659bc, ; 52: Xamarin.AndroidX.Navigation.UI => 95
	i64 u0x1e71143913d56c10, ; 53: lib-ko-Microsoft.Maui.Controls.resources.dll.so => 17
	i64 u0x1ed8fcce5e9b50a0, ; 54: Microsoft.Extensions.Options.dll => 63
	i64 u0x209375905fcc1bad, ; 55: lib_System.IO.Compression.Brotli.dll.so => 127
	i64 u0x2174319c0d835bc9, ; 56: System.Runtime => 148
	i64 u0x220fd4f2e7c48170, ; 57: th/Microsoft.Maui.Controls.resources => 28
	i64 u0x236b0c00e2f0397d, ; 58: lib_FokusApp.Migrations.dll.so => 107
	i64 u0x237be844f1f812c7, ; 59: System.Threading.Thread.dll => 153
	i64 u0x23807c59646ec4f3, ; 60: lib_Microsoft.EntityFrameworkCore.dll.so => 42
	i64 u0x2407aef2bbe8fadf, ; 61: System.Console => 119
	i64 u0x240abe014b27e7d3, ; 62: Xamarin.AndroidX.Core.dll => 82
	i64 u0x252073cc3caa62c2, ; 63: fr/Microsoft.Maui.Controls.resources.dll => 9
	i64 u0x25a0a7eff76ea08e, ; 64: SQLitePCLRaw.batteries_v2.dll => 72
	i64 u0x2662c629b96b0b30, ; 65: lib_Xamarin.Kotlin.StdLib.dll.so => 103
	i64 u0x268c1439f13bcc29, ; 66: lib_Microsoft.Extensions.Primitives.dll.so => 64
	i64 u0x273f3515de5faf0d, ; 67: id/Microsoft.Maui.Controls.resources.dll => 14
	i64 u0x2742545f9094896d, ; 68: hr/Microsoft.Maui.Controls.resources => 12
	i64 u0x27b410442fad6cf1, ; 69: Java.Interop.dll => 160
	i64 u0x2801845a2c71fbfb, ; 70: System.Net.Primitives.dll => 137
	i64 u0x28e52865585a1ebe, ; 71: Microsoft.Extensions.Diagnostics.Abstractions.dll => 53
	i64 u0x29aeab763a527e52, ; 72: lib_Xamarin.AndroidX.Navigation.Common.Android.dll.so => 92
	i64 u0x2a128783efe70ba0, ; 73: uk/Microsoft.Maui.Controls.resources.dll => 30
	i64 u0x2a3b095612184159, ; 74: lib_System.Net.NetworkInformation.dll.so => 136
	i64 u0x2a6507a5ffabdf28, ; 75: System.Diagnostics.TraceSource.dll => 122
	i64 u0x2ad156c8e1354139, ; 76: fi/Microsoft.Maui.Controls.resources => 8
	i64 u0x2af298f63581d886, ; 77: System.Text.RegularExpressions.dll => 152
	i64 u0x2afc1c4f898552ee, ; 78: lib_System.Formats.Asn1.dll.so => 126
	i64 u0x2b148910ed40fbf9, ; 79: zh-Hant/Microsoft.Maui.Controls.resources.dll => 34
	i64 u0x2b4d4904cebfa4e9, ; 80: Microsoft.Extensions.FileSystemGlobbing => 57
	i64 u0x2c8bd14bb93a7d82, ; 81: lib-pl-Microsoft.Maui.Controls.resources.dll.so => 21
	i64 u0x2d169d318a968379, ; 82: System.Threading.dll => 154
	i64 u0x2d47774b7d993f59, ; 83: sv/Microsoft.Maui.Controls.resources.dll => 27
	i64 u0x2db915caf23548d2, ; 84: System.Text.Json.dll => 151
	i64 u0x2e6f1f226821322a, ; 85: el/Microsoft.Maui.Controls.resources.dll => 6
	i64 u0x2e8ff3fae87a8245, ; 86: lib_Microsoft.JSInterop.dll.so => 65
	i64 u0x2f2e98e1c89b1aff, ; 87: System.Xml.ReaderWriter => 156
	i64 u0x2f5911d9ba814e4e, ; 88: System.Diagnostics.Tracing => 123
	i64 u0x2feb4d2fcda05cfd, ; 89: Microsoft.Extensions.Caching.Abstractions.dll => 46
	i64 u0x309ee9eeec09a71e, ; 90: lib_Xamarin.AndroidX.Fragment.dll.so => 86
	i64 u0x31195fef5d8fb552, ; 91: _Microsoft.Android.Resource.Designer.dll => 35
	i64 u0x32243413e774362a, ; 92: Xamarin.AndroidX.CardView.dll => 79
	i64 u0x3235427f8d12dae1, ; 93: lib_System.Drawing.Primitives.dll.so => 124
	i64 u0x329753a17a517811, ; 94: fr/Microsoft.Maui.Controls.resources => 9
	i64 u0x32aa989ff07a84ff, ; 95: lib_System.Xml.ReaderWriter.dll.so => 156
	i64 u0x33642d5508314e46, ; 96: Microsoft.Extensions.FileSystemGlobbing.dll => 57
	i64 u0x33829542f112d59b, ; 97: System.Collections.Immutable => 111
	i64 u0x33a31443733849fe, ; 98: lib-es-Microsoft.Maui.Controls.resources.dll.so => 7
	i64 u0x34bd01fd4be06ee3, ; 99: lib_Microsoft.Extensions.FileProviders.Composite.dll.so => 55
	i64 u0x34dfd74fe2afcf37, ; 100: Microsoft.Maui => 68
	i64 u0x34e292762d9615df, ; 101: cs/Microsoft.Maui.Controls.resources.dll => 3
	i64 u0x3508234247f48404, ; 102: Microsoft.Maui.Controls => 66
	i64 u0x353590da528c9d22, ; 103: System.ComponentModel.Annotations => 115
	i64 u0x3549870798b4cd30, ; 104: lib_Xamarin.AndroidX.ViewPager2.dll.so => 101
	i64 u0x355282fc1c909694, ; 105: Microsoft.Extensions.Configuration => 48
	i64 u0x380134e03b1e160a, ; 106: System.Collections.Immutable.dll => 111
	i64 u0x385c17636bb6fe6e, ; 107: Xamarin.AndroidX.CustomView.dll => 84
	i64 u0x393c226616977fdb, ; 108: lib_Xamarin.AndroidX.ViewPager.dll.so => 100
	i64 u0x395e37c3334cf82a, ; 109: lib-ca-Microsoft.Maui.Controls.resources.dll.so => 2
	i64 u0x39c3107c28752af1, ; 110: lib_Microsoft.Extensions.FileProviders.Abstractions.dll.so => 54
	i64 u0x3be6248c2bc7dc8c, ; 111: Microsoft.JSInterop.dll => 65
	i64 u0x3be99b43dd39dd37, ; 112: Xamarin.AndroidX.SavedState.SavedState.Android => 97
	i64 u0x3c7c495f58ac5ee9, ; 113: Xamarin.Kotlin.StdLib => 103
	i64 u0x3cdce1f9610469be, ; 114: FokusApp.Migrations => 107
	i64 u0x3d9c2a242b040a50, ; 115: lib_Xamarin.AndroidX.Core.dll.so => 82
	i64 u0x3da7781d6333a8fe, ; 116: SQLitePCLRaw.batteries_v2 => 72
	i64 u0x3e7f8912b96e5065, ; 117: Microsoft.AspNetCore.Components.WebView.dll => 39
	i64 u0x3f6f5914291cdcf7, ; 118: Microsoft.Extensions.Hosting.Abstractions => 58
	i64 u0x41cab042be111c34, ; 119: lib_Xamarin.AndroidX.AppCompat.AppCompatResources.dll.so => 78
	i64 u0x41f93add55d80a27, ; 120: lib_Microsoft.Extensions.Localization.dll.so => 59
	i64 u0x43375950ec7c1b6a, ; 121: netstandard.dll => 158
	i64 u0x434c4e1d9284cdae, ; 122: Mono.Android.dll => 163
	i64 u0x43950f84de7cc79a, ; 123: pl/Microsoft.Maui.Controls.resources.dll => 21
	i64 u0x439cd2a34ac0cca1, ; 124: FokusApp.dll => 109
	i64 u0x4515080865a951a5, ; 125: Xamarin.Kotlin.StdLib.dll => 103
	i64 u0x453c1277f85cf368, ; 126: lib_Microsoft.EntityFrameworkCore.Abstractions.dll.so => 43
	i64 u0x45c40276a42e283e, ; 127: System.Diagnostics.TraceSource => 122
	i64 u0x45fcc9fd66f25095, ; 128: Microsoft.Extensions.DependencyModel => 52
	i64 u0x46a4213bc97fe5ae, ; 129: lib-ru-Microsoft.Maui.Controls.resources.dll.so => 25
	i64 u0x47daf4e1afbada10, ; 130: pt/Microsoft.Maui.Controls.resources => 23
	i64 u0x49e952f19a4e2022, ; 131: System.ObjectModel => 140
	i64 u0x49f61f655a6a21de, ; 132: Microsoft.Extensions.Localization.Abstractions.dll => 60
	i64 u0x49f9e6948a8131e4, ; 133: lib_Xamarin.AndroidX.VersionedParcelable.dll.so => 99
	i64 u0x4a5667b2462a664b, ; 134: lib_Xamarin.AndroidX.Navigation.UI.dll.so => 95
	i64 u0x4b7b6532ded934b7, ; 135: System.Text.Json => 151
	i64 u0x4c2029a97af23a8d, ; 136: Xamarin.AndroidX.Lifecycle.ViewModelSavedState.Android => 90
	i64 u0x4ca014ceac582c86, ; 137: Microsoft.EntityFrameworkCore.Relational.dll => 44
	i64 u0x4cc5f15266470798, ; 138: lib_Xamarin.AndroidX.Loader.dll.so => 91
	i64 u0x4cf6f67dc77aacd2, ; 139: System.Net.NetworkInformation.dll => 136
	i64 u0x4d479f968a05e504, ; 140: System.Linq.Expressions.dll => 131
	i64 u0x4d55a010ffc4faff, ; 141: System.Private.Xml => 142
	i64 u0x4d95fccc1f67c7ca, ; 142: System.Runtime.Loader.dll => 145
	i64 u0x4dcf44c3c9b076a2, ; 143: it/Microsoft.Maui.Controls.resources.dll => 15
	i64 u0x4dd9247f1d2c3235, ; 144: Xamarin.AndroidX.Loader.dll => 91
	i64 u0x4df510084e2a0bae, ; 145: Microsoft.JSInterop => 65
	i64 u0x4e32f00cb0937401, ; 146: Mono.Android.Runtime => 162
	i64 u0x4f21ee6ef9eb527e, ; 147: ca/Microsoft.Maui.Controls.resources => 2
	i64 u0x4fd5f3ee53d0a4f0, ; 148: SQLitePCLRaw.lib.e_sqlite3.android => 74
	i64 u0x5037f0be3c28c7a3, ; 149: lib_Microsoft.Maui.Controls.dll.so => 66
	i64 u0x506586cf3ba5b629, ; 150: FokusApp.Presentation => 108
	i64 u0x5112ed116d87baf8, ; 151: CommunityToolkit.Mvvm => 36
	i64 u0x5131bbe80989093f, ; 152: Xamarin.AndroidX.Lifecycle.ViewModel.Android.dll => 89
	i64 u0x51bb8a2afe774e32, ; 153: System.Drawing => 125
	i64 u0x526ce79eb8e90527, ; 154: lib_System.Net.Primitives.dll.so => 137
	i64 u0x52829f00b4467c38, ; 155: lib_System.Data.Common.dll.so => 120
	i64 u0x529ffe06f39ab8db, ; 156: Xamarin.AndroidX.Core => 82
	i64 u0x52ff996554dbf352, ; 157: Microsoft.Maui.Graphics => 70
	i64 u0x535f7e40e8fef8af, ; 158: lib-sk-Microsoft.Maui.Controls.resources.dll.so => 26
	i64 u0x53a96d5c86c9e194, ; 159: System.Net.NetworkInformation => 136
	i64 u0x53be1038a61e8d44, ; 160: System.Runtime.InteropServices.RuntimeInformation.dll => 143
	i64 u0x53c3014b9437e684, ; 161: lib-zh-HK-Microsoft.Maui.Controls.resources.dll.so => 32
	i64 u0x54795225dd1587af, ; 162: lib_System.Runtime.dll.so => 148
	i64 u0x54b851bc9b470503, ; 163: Xamarin.AndroidX.Navigation.Common.Android => 92
	i64 u0x556e8b63b660ab8b, ; 164: Xamarin.AndroidX.Lifecycle.Common.Jvm.dll => 87
	i64 u0x5588627c9a108ec9, ; 165: System.Collections.Specialized => 113
	i64 u0x571c5cfbec5ae8e2, ; 166: System.Private.Uri => 141
	i64 u0x578cd35c91d7b347, ; 167: lib_SQLitePCLRaw.core.dll.so => 73
	i64 u0x579a06fed6eec900, ; 168: System.Private.CoreLib.dll => 159
	i64 u0x57adda3c951abb33, ; 169: Microsoft.Extensions.Hosting.Abstractions.dll => 58
	i64 u0x57c542c14049b66d, ; 170: System.Diagnostics.DiagnosticSource => 121
	i64 u0x58601b2dda4a27b9, ; 171: lib-ja-Microsoft.Maui.Controls.resources.dll.so => 16
	i64 u0x58688d9af496b168, ; 172: Microsoft.Extensions.DependencyInjection.dll => 50
	i64 u0x5928b11c2a4b3b58, ; 173: lib_FokusApp.dll.so => 109
	i64 u0x5a89a886ae30258d, ; 174: lib_Xamarin.AndroidX.CoordinatorLayout.dll.so => 81
	i64 u0x5a8f6699f4a1caa9, ; 175: lib_System.Threading.dll.so => 154
	i64 u0x5ae9cd33b15841bf, ; 176: System.ComponentModel => 118
	i64 u0x5b5f0e240a06a2a2, ; 177: da/Microsoft.Maui.Controls.resources.dll => 4
	i64 u0x5c393624b8176517, ; 178: lib_Microsoft.Extensions.Logging.dll.so => 61
	i64 u0x5d25ef991dd9a85c, ; 179: Microsoft.AspNetCore.Components.WebView.Maui.dll => 40
	i64 u0x5db0cbbd1028510e, ; 180: lib_System.Runtime.InteropServices.dll.so => 144
	i64 u0x5db30905d3e5013b, ; 181: Xamarin.AndroidX.Collection.Jvm.dll => 80
	i64 u0x5e467bc8f09ad026, ; 182: System.Collections.Specialized.dll => 113
	i64 u0x5ea92fdb19ec8c4c, ; 183: System.Text.Encodings.Web.dll => 150
	i64 u0x5eb8046dd40e9ac3, ; 184: System.ComponentModel.Primitives => 116
	i64 u0x5f36ccf5c6a57e24, ; 185: System.Xml.ReaderWriter.dll => 156
	i64 u0x5f4294b9b63cb842, ; 186: System.Data.Common => 120
	i64 u0x5f7399e166075632, ; 187: lib_SQLitePCLRaw.lib.e_sqlite3.android.dll.so => 74
	i64 u0x5f9a2d823f664957, ; 188: lib-el-Microsoft.Maui.Controls.resources.dll.so => 6
	i64 u0x609f4b7b63d802d4, ; 189: lib_Microsoft.Extensions.DependencyInjection.dll.so => 50
	i64 u0x60cd4e33d7e60134, ; 190: Xamarin.KotlinX.Coroutines.Core.Jvm => 104
	i64 u0x60f62d786afcf130, ; 191: System.Memory => 134
	i64 u0x61be8d1299194243, ; 192: Microsoft.Maui.Controls.Xaml => 67
	i64 u0x61d2cba29557038f, ; 193: de/Microsoft.Maui.Controls.resources => 5
	i64 u0x61d88f399afb2f45, ; 194: lib_System.Runtime.Loader.dll.so => 145
	i64 u0x622eef6f9e59068d, ; 195: System.Private.CoreLib => 159
	i64 u0x639fb99a7bef11de, ; 196: Xamarin.AndroidX.Navigation.Runtime.Android.dll => 94
	i64 u0x63f1f6883c1e23c2, ; 197: lib_System.Collections.Immutable.dll.so => 111
	i64 u0x6400f68068c1e9f1, ; 198: Xamarin.Google.Android.Material.dll => 102
	i64 u0x65ecac39144dd3cc, ; 199: Microsoft.Maui.Controls.dll => 66
	i64 u0x65ece51227bfa724, ; 200: lib_System.Runtime.Numerics.dll.so => 146
	i64 u0x6692e924eade1b29, ; 201: lib_System.Console.dll.so => 119
	i64 u0x66a4e5c6a3fb0bae, ; 202: lib_Xamarin.AndroidX.Lifecycle.ViewModel.Android.dll.so => 89
	i64 u0x66d13304ce1a3efa, ; 203: Xamarin.AndroidX.CursorAdapter => 83
	i64 u0x68558ec653afa616, ; 204: lib-da-Microsoft.Maui.Controls.resources.dll.so => 4
	i64 u0x6872ec7a2e36b1ac, ; 205: System.Drawing.Primitives.dll => 124
	i64 u0x68fbbbe2eb455198, ; 206: System.Formats.Asn1 => 126
	i64 u0x69063fc0ba8e6bdd, ; 207: he/Microsoft.Maui.Controls.resources.dll => 10
	i64 u0x699dffb2427a2d71, ; 208: SQLitePCLRaw.lib.e_sqlite3.android.dll => 74
	i64 u0x6a4d7577b2317255, ; 209: System.Runtime.InteropServices.dll => 144
	i64 u0x6ace3b74b15ee4a4, ; 210: nb/Microsoft.Maui.Controls.resources => 19
	i64 u0x6c46bd19605219e3, ; 211: Microsoft.Extensions.Localization => 59
	i64 u0x6d12bfaa99c72b1f, ; 212: lib_Microsoft.Maui.Graphics.dll.so => 70
	i64 u0x6d79993361e10ef2, ; 213: Microsoft.Extensions.Primitives => 64
	i64 u0x6d86d56b84c8eb71, ; 214: lib_Xamarin.AndroidX.CursorAdapter.dll.so => 83
	i64 u0x6d9bea6b3e895cf7, ; 215: Microsoft.Extensions.Primitives.dll => 64
	i64 u0x6e25a02c3833319a, ; 216: lib_Xamarin.AndroidX.Navigation.Fragment.dll.so => 93
	i64 u0x6fd2265da78b93a4, ; 217: lib_Microsoft.Maui.dll.so => 68
	i64 u0x6fdfc7de82c33008, ; 218: cs/Microsoft.Maui.Controls.resources => 3
	i64 u0x6ffc4967cc47ba57, ; 219: System.IO.FileSystem.Watcher.dll => 129
	i64 u0x70e99f48c05cb921, ; 220: tr/Microsoft.Maui.Controls.resources.dll => 29
	i64 u0x70fd3deda22442d2, ; 221: lib-nb-Microsoft.Maui.Controls.resources.dll.so => 19
	i64 u0x717530326f808838, ; 222: lib_Microsoft.Extensions.Diagnostics.Abstractions.dll.so => 53
	i64 u0x71a495ea3761dde8, ; 223: lib-it-Microsoft.Maui.Controls.resources.dll.so => 15
	i64 u0x71ad672adbe48f35, ; 224: System.ComponentModel.Primitives.dll => 116
	i64 u0x72b1fb4109e08d7b, ; 225: lib-hr-Microsoft.Maui.Controls.resources.dll.so => 12
	i64 u0x73e4ce94e2eb6ffc, ; 226: lib_System.Memory.dll.so => 134
	i64 u0x73f2645914262879, ; 227: lib_Microsoft.EntityFrameworkCore.Sqlite.dll.so => 45
	i64 u0x755a91767330b3d4, ; 228: lib_Microsoft.Extensions.Configuration.dll.so => 48
	i64 u0x76ca07b878f44da0, ; 229: System.Runtime.Numerics.dll => 146
	i64 u0x780bc73597a503a9, ; 230: lib-ms-Microsoft.Maui.Controls.resources.dll.so => 18
	i64 u0x783606d1e53e7a1a, ; 231: th/Microsoft.Maui.Controls.resources.dll => 28
	i64 u0x78a45e51311409b6, ; 232: Xamarin.AndroidX.Fragment.dll => 86
	i64 u0x7a71889545dcdb00, ; 233: lib_Microsoft.AspNetCore.Components.WebView.dll.so => 39
	i64 u0x7adb8da2ac89b647, ; 234: fi/Microsoft.Maui.Controls.resources.dll => 8
	i64 u0x7b150145c0a9058c, ; 235: Microsoft.Data.Sqlite => 41
	i64 u0x7bef86a4335c4870, ; 236: System.ComponentModel.TypeConverter => 117
	i64 u0x7c0820144cd34d6a, ; 237: sk/Microsoft.Maui.Controls.resources.dll => 26
	i64 u0x7c2a0bd1e0f988fc, ; 238: lib-de-Microsoft.Maui.Controls.resources.dll.so => 5
	i64 u0x7c60acf6404e96b6, ; 239: Xamarin.AndroidX.Navigation.Common.Android.dll => 92
	i64 u0x7d649b75d580bb42, ; 240: ms/Microsoft.Maui.Controls.resources.dll => 18
	i64 u0x7d8ee2bdc8e3aad1, ; 241: System.Numerics.Vectors => 139
	i64 u0x7dfc3d6d9d8d7b70, ; 242: System.Collections => 114
	i64 u0x7e2e564fa2f76c65, ; 243: lib_System.Diagnostics.Tracing.dll.so => 123
	i64 u0x7e946809d6008ef2, ; 244: lib_System.ObjectModel.dll.so => 140
	i64 u0x7ecc13347c8fd849, ; 245: lib_System.ComponentModel.dll.so => 118
	i64 u0x7f00ddd9b9ca5a13, ; 246: Xamarin.AndroidX.ViewPager.dll => 100
	i64 u0x7f9351cd44b1273f, ; 247: Microsoft.Extensions.Configuration.Abstractions => 49
	i64 u0x7fbd557c99b3ce6f, ; 248: lib_Xamarin.AndroidX.Lifecycle.LiveData.Core.dll.so => 88
	i64 u0x809b1d586a1c34b9, ; 249: Plugin.Maui.Audio => 71
	i64 u0x80fa55b6d1b0be99, ; 250: SQLitePCLRaw.provider.e_sqlite3 => 75
	i64 u0x8101a73bd4533440, ; 251: Microsoft.AspNetCore.Components.Web => 38
	i64 u0x812c069d5cdecc17, ; 252: System.dll => 157
	i64 u0x8148a1fb34fceb7c, ; 253: Microsoft.Extensions.Localization.Abstractions => 60
	i64 u0x818288c342ac6ea5, ; 254: lib-en-FokusApp.resources.dll.so => 0
	i64 u0x81ab745f6c0f5ce6, ; 255: zh-Hant/Microsoft.Maui.Controls.resources => 34
	i64 u0x8277f2be6b5ce05f, ; 256: Xamarin.AndroidX.AppCompat => 77
	i64 u0x828f06563b30bc50, ; 257: lib_Xamarin.AndroidX.CardView.dll.so => 79
	i64 u0x82df8f5532a10c59, ; 258: lib_System.Drawing.dll.so => 125
	i64 u0x82f6403342e12049, ; 259: uk/Microsoft.Maui.Controls.resources => 30
	i64 u0x83c14ba66c8e2b8c, ; 260: zh-Hans/Microsoft.Maui.Controls.resources => 33
	i64 u0x83de69860da6cbdd, ; 261: Microsoft.Extensions.FileProviders.Composite => 55
	i64 u0x84cd5cdec0f54bcc, ; 262: lib_Microsoft.EntityFrameworkCore.Relational.dll.so => 44
	i64 u0x852955234ef97304, ; 263: FokusApp.Presentation.dll => 108
	i64 u0x86a909228dc7657b, ; 264: lib-zh-Hant-Microsoft.Maui.Controls.resources.dll.so => 34
	i64 u0x86b3e00c36b84509, ; 265: Microsoft.Extensions.Configuration.dll => 48
	i64 u0x8704193f462e892e, ; 266: lib_Microsoft.Extensions.FileSystemGlobbing.dll.so => 57
	i64 u0x87c4b8a492b176ad, ; 267: Microsoft.EntityFrameworkCore.Abstractions => 43
	i64 u0x87c69b87d9283884, ; 268: lib_System.Threading.Thread.dll.so => 153
	i64 u0x87f6569b25707834, ; 269: System.IO.Compression.Brotli.dll => 127
	i64 u0x8842b3a5d2d3fb36, ; 270: Microsoft.Maui.Essentials => 69
	i64 u0x88bda98e0cffb7a9, ; 271: lib_Xamarin.KotlinX.Coroutines.Core.Jvm.dll.so => 104
	i64 u0x8930322c7bd8f768, ; 272: netstandard => 158
	i64 u0x897a606c9e39c75f, ; 273: lib_System.ComponentModel.Primitives.dll.so => 116
	i64 u0x898a5c6bc9e47ec1, ; 274: lib_Xamarin.AndroidX.SavedState.SavedState.Android.dll.so => 97
	i64 u0x89c5188089ec2cd5, ; 275: lib_System.Runtime.InteropServices.RuntimeInformation.dll.so => 143
	i64 u0x8a399a706fcbce4b, ; 276: Microsoft.Extensions.Caching.Abstractions => 46
	i64 u0x8ad229ea26432ee2, ; 277: Xamarin.AndroidX.Loader => 91
	i64 u0x8b4ff5d0fdd5faa1, ; 278: lib_System.Diagnostics.DiagnosticSource.dll.so => 121
	i64 u0x8b9ceca7acae3451, ; 279: lib-he-Microsoft.Maui.Controls.resources.dll.so => 10
	i64 u0x8c575135aa1ccef4, ; 280: Microsoft.Extensions.FileProviders.Abstractions => 54
	i64 u0x8d0f420977c2c1c7, ; 281: Xamarin.AndroidX.CursorAdapter.dll => 83
	i64 u0x8d52a25632e81824, ; 282: Microsoft.EntityFrameworkCore.Sqlite.dll => 45
	i64 u0x8d7b8ab4b3310ead, ; 283: System.Threading => 154
	i64 u0x8da188285aadfe8e, ; 284: System.Collections.Concurrent => 110
	i64 u0x8ee08b8194a30f48, ; 285: lib-hi-Microsoft.Maui.Controls.resources.dll.so => 11
	i64 u0x8ef7601039857a44, ; 286: lib-ro-Microsoft.Maui.Controls.resources.dll.so => 24
	i64 u0x8ef9414937d93a0a, ; 287: SQLitePCLRaw.core.dll => 73
	i64 u0x8f32c6f611f6ffab, ; 288: pt/Microsoft.Maui.Controls.resources.dll => 23
	i64 u0x8f8829d21c8985a4, ; 289: lib-pt-BR-Microsoft.Maui.Controls.resources.dll.so => 22
	i64 u0x8fd27d934d7b3a55, ; 290: SQLitePCLRaw.core => 73
	i64 u0x90263f8448b8f572, ; 291: lib_System.Diagnostics.TraceSource.dll.so => 122
	i64 u0x903101b46fb73a04, ; 292: _Microsoft.Android.Resource.Designer => 35
	i64 u0x90393bd4865292f3, ; 293: lib_System.IO.Compression.dll.so => 128
	i64 u0x90634f86c5ebe2b5, ; 294: Xamarin.AndroidX.Lifecycle.ViewModel.Android => 89
	i64 u0x907b636704ad79ef, ; 295: lib_Microsoft.Maui.Controls.Xaml.dll.so => 67
	i64 u0x91418dc638b29e68, ; 296: lib_Xamarin.AndroidX.CustomView.dll.so => 84
	i64 u0x9157bd523cd7ed36, ; 297: lib_System.Text.Json.dll.so => 151
	i64 u0x91a74f07b30d37e2, ; 298: System.Linq.dll => 133
	i64 u0x91fa41a87223399f, ; 299: ca/Microsoft.Maui.Controls.resources.dll => 2
	i64 u0x93cfa73ab28d6e35, ; 300: ms/Microsoft.Maui.Controls.resources => 18
	i64 u0x944077d8ca3c6580, ; 301: System.IO.Compression.dll => 128
	i64 u0x94e68b2add1bcd0d, ; 302: en/FokusApp.resources => 0
	i64 u0x967fc325e09bfa8c, ; 303: es/Microsoft.Maui.Controls.resources => 7
	i64 u0x9732d8dbddea3d9a, ; 304: id/Microsoft.Maui.Controls.resources => 14
	i64 u0x978be80e5210d31b, ; 305: Microsoft.Maui.Graphics.dll => 70
	i64 u0x97b8c771ea3e4220, ; 306: System.ComponentModel.dll => 118
	i64 u0x97e144c9d3c6976e, ; 307: System.Collections.Concurrent.dll => 110
	i64 u0x98b05cc81e6f333c, ; 308: Xamarin.AndroidX.SavedState.SavedState.Android.dll => 97
	i64 u0x991d510397f92d9d, ; 309: System.Linq.Expressions => 131
	i64 u0x999cb19e1a04ffd3, ; 310: CommunityToolkit.Mvvm.dll => 36
	i64 u0x99cdc6d1f2d3a72f, ; 311: ko/Microsoft.Maui.Controls.resources.dll => 17
	i64 u0x9b211a749105beac, ; 312: System.Transactions.Local => 155
	i64 u0x9d1f6cd40094e12b, ; 313: lib_FokusApp.Core.dll.so => 106
	i64 u0x9d5dbcf5a48583fe, ; 314: lib_Xamarin.AndroidX.Activity.dll.so => 76
	i64 u0x9d74dee1a7725f34, ; 315: Microsoft.Extensions.Configuration.Abstractions.dll => 49
	i64 u0x9dd0e195825d65c6, ; 316: lib_Xamarin.AndroidX.Navigation.Runtime.Android.dll.so => 94
	i64 u0x9e4534b6adaf6e84, ; 317: nl/Microsoft.Maui.Controls.resources => 20
	i64 u0x9ef542cf1f78c506, ; 318: Xamarin.AndroidX.Lifecycle.LiveData.Core => 88
	i64 u0x9fbb2961ca18e5c2, ; 319: Microsoft.Extensions.FileProviders.Physical.dll => 56
	i64 u0xa0d8259f4cc284ec, ; 320: lib_System.Security.Cryptography.dll.so => 149
	i64 u0xa1440773ee9d341e, ; 321: Xamarin.Google.Android.Material => 102
	i64 u0xa1b9d7c27f47219f, ; 322: Xamarin.AndroidX.Navigation.UI.dll => 95
	i64 u0xa2572680829d2c7c, ; 323: System.IO.Pipelines.dll => 130
	i64 u0xa46aa1eaa214539b, ; 324: ko/Microsoft.Maui.Controls.resources => 17
	i64 u0xa4d20d2ff0563d26, ; 325: lib_CommunityToolkit.Mvvm.dll.so => 36
	i64 u0xa4edc8f2ceae241a, ; 326: System.Data.Common.dll => 120
	i64 u0xa5494f40f128ce6a, ; 327: System.Runtime.Serialization.Formatters.dll => 147
	i64 u0xa5b7152421ed6d98, ; 328: lib_System.IO.FileSystem.Watcher.dll.so => 129
	i64 u0xa5e599d1e0524750, ; 329: System.Numerics.Vectors.dll => 139
	i64 u0xa5f1ba49b85dd355, ; 330: System.Security.Cryptography.dll => 149
	i64 u0xa68a420042bb9b1f, ; 331: Xamarin.AndroidX.DrawerLayout.dll => 85
	i64 u0xa78ce3745383236a, ; 332: Xamarin.AndroidX.Lifecycle.Common.Jvm => 87
	i64 u0xa7c31b56b4dc7b33, ; 333: hu/Microsoft.Maui.Controls.resources => 13
	i64 u0xa7eab29ed44b4e7a, ; 334: Mono.Android.Export => 161
	i64 u0xa82fd211eef00a5b, ; 335: Microsoft.Extensions.FileProviders.Physical => 56
	i64 u0xaa2219c8e3449ff5, ; 336: Microsoft.Extensions.Logging.Abstractions => 62
	i64 u0xaa443ac34067eeef, ; 337: System.Private.Xml.dll => 142
	i64 u0xaa52de307ef5d1dd, ; 338: System.Net.Http => 135
	i64 u0xaaaf86367285a918, ; 339: Microsoft.Extensions.DependencyInjection.Abstractions.dll => 51
	i64 u0xaaf84bb3f052a265, ; 340: el/Microsoft.Maui.Controls.resources => 6
	i64 u0xab9c1b2687d86b0b, ; 341: lib_System.Linq.Expressions.dll.so => 131
	i64 u0xac2af3fa195a15ce, ; 342: System.Runtime.Numerics => 146
	i64 u0xac5376a2a538dc10, ; 343: Xamarin.AndroidX.Lifecycle.LiveData.Core.dll => 88
	i64 u0xacd46e002c3ccb97, ; 344: ro/Microsoft.Maui.Controls.resources => 24
	i64 u0xad89c07347f1bad6, ; 345: nl/Microsoft.Maui.Controls.resources.dll => 20
	i64 u0xadc90ab061a9e6e4, ; 346: System.ComponentModel.TypeConverter.dll => 117
	i64 u0xae282bcd03739de7, ; 347: Java.Interop => 160
	i64 u0xae53579c90db1107, ; 348: System.ObjectModel.dll => 140
	i64 u0xaf12fb8133ac3fbb, ; 349: Microsoft.EntityFrameworkCore.Sqlite => 45
	i64 u0xb05cc42cd94c6d9d, ; 350: lib-sv-Microsoft.Maui.Controls.resources.dll.so => 27
	i64 u0xb0bb43dc52ea59f9, ; 351: System.Diagnostics.Tracing.dll => 123
	i64 u0xb1ccbf6243328d1c, ; 352: Microsoft.AspNetCore.Components => 37
	i64 u0xb220631954820169, ; 353: System.Text.RegularExpressions => 152
	i64 u0xb2a3f67f3bf29fce, ; 354: da/Microsoft.Maui.Controls.resources => 4
	i64 u0xb3f0a0fcda8d3ebc, ; 355: Xamarin.AndroidX.CardView => 79
	i64 u0xb46be1aa6d4fff93, ; 356: hi/Microsoft.Maui.Controls.resources => 11
	i64 u0xb477491be13109d8, ; 357: ar/Microsoft.Maui.Controls.resources => 1
	i64 u0xb4bd7015ecee9d86, ; 358: System.IO.Pipelines => 130
	i64 u0xb5c7fcdafbc67ee4, ; 359: Microsoft.Extensions.Logging.Abstractions.dll => 62
	i64 u0xb7212c4683a94afe, ; 360: System.Drawing.Primitives => 124
	i64 u0xb7b7753d1f319409, ; 361: sv/Microsoft.Maui.Controls.resources => 27
	i64 u0xb81a2c6e0aee50fe, ; 362: lib_System.Private.CoreLib.dll.so => 159
	i64 u0xb960d6b2200ba320, ; 363: Xamarin.AndroidX.Lifecycle.ViewModelSavedState.Android.dll => 90
	i64 u0xb9f64d3b230def68, ; 364: lib-pt-Microsoft.Maui.Controls.resources.dll.so => 23
	i64 u0xb9fc3c8a556e3691, ; 365: ja/Microsoft.Maui.Controls.resources => 16
	i64 u0xba48785529705af9, ; 366: System.Collections.dll => 114
	i64 u0xbaf762c4825c14e9, ; 367: Microsoft.AspNetCore.Components.WebView => 39
	i64 u0xbbd180354b67271a, ; 368: System.Runtime.Serialization.Formatters => 147
	i64 u0xbc22a245dab70cb4, ; 369: lib_SQLitePCLRaw.provider.e_sqlite3.dll.so => 75
	i64 u0xbd0e2c0d55246576, ; 370: System.Net.Http.dll => 135
	i64 u0xbd437a2cdb333d0d, ; 371: Xamarin.AndroidX.ViewPager2 => 101
	i64 u0xbee38d4a88835966, ; 372: Xamarin.AndroidX.AppCompat.AppCompatResources => 78
	i64 u0xc040a4ab55817f58, ; 373: ar/Microsoft.Maui.Controls.resources.dll => 1
	i64 u0xc0d928351ab5ca77, ; 374: System.Console.dll => 119
	i64 u0xc12b8b3afa48329c, ; 375: lib_System.Linq.dll.so => 133
	i64 u0xc1c2cb7af77b8858, ; 376: Microsoft.EntityFrameworkCore => 42
	i64 u0xc1ff9ae3cdb6e1e6, ; 377: Xamarin.AndroidX.Activity.dll => 76
	i64 u0xc28c50f32f81cc73, ; 378: ja/Microsoft.Maui.Controls.resources.dll => 16
	i64 u0xc2902f6cf5452577, ; 379: lib_Mono.Android.Export.dll.so => 161
	i64 u0xc2a3bca55b573141, ; 380: System.IO.FileSystem.Watcher => 129
	i64 u0xc2bcfec99f69365e, ; 381: Xamarin.AndroidX.ViewPager2.dll => 101
	i64 u0xc3492f8f90f96ce4, ; 382: lib_Microsoft.Extensions.DependencyModel.dll.so => 52
	i64 u0xc3cc416033a7b1b7, ; 383: FokusApp => 109
	i64 u0xc472ce300460ccb6, ; 384: Microsoft.EntityFrameworkCore.dll => 42
	i64 u0xc4d69851fe06342f, ; 385: lib_Microsoft.Extensions.Caching.Memory.dll.so => 47
	i64 u0xc50fded0ded1418c, ; 386: lib_System.ComponentModel.TypeConverter.dll.so => 117
	i64 u0xc519125d6bc8fb11, ; 387: lib_System.Net.Requests.dll.so => 138
	i64 u0xc5293b19e4dc230e, ; 388: Xamarin.AndroidX.Navigation.Fragment => 93
	i64 u0xc5325b2fcb37446f, ; 389: lib_System.Private.Xml.dll.so => 142
	i64 u0xc5a0f4b95a699af7, ; 390: lib_System.Private.Uri.dll.so => 141
	i64 u0xc74d70d4aa96cef3, ; 391: Xamarin.AndroidX.Navigation.Runtime.Android => 94
	i64 u0xc858a28d9ee5a6c5, ; 392: lib_System.Collections.Specialized.dll.so => 113
	i64 u0xca3110fea81c8916, ; 393: Microsoft.AspNetCore.Components.Web.dll => 38
	i64 u0xca32340d8d54dcd5, ; 394: Microsoft.Extensions.Caching.Memory.dll => 47
	i64 u0xca3a723e7342c5b6, ; 395: lib-tr-Microsoft.Maui.Controls.resources.dll.so => 29
	i64 u0xcab3493c70141c2d, ; 396: pl/Microsoft.Maui.Controls.resources => 21
	i64 u0xcacfddc9f7c6de76, ; 397: ro/Microsoft.Maui.Controls.resources.dll => 24
	i64 u0xcb45618372c47127, ; 398: Microsoft.EntityFrameworkCore.Relational => 44
	i64 u0xcbd4fdd9cef4a294, ; 399: lib__Microsoft.Android.Resource.Designer.dll.so => 35
	i64 u0xcc2876b32ef2794c, ; 400: lib_System.Text.RegularExpressions.dll.so => 152
	i64 u0xcc5c3bb714c4561e, ; 401: Xamarin.KotlinX.Coroutines.Core.Jvm.dll => 104
	i64 u0xcc76886e09b88260, ; 402: Xamarin.KotlinX.Serialization.Core.Jvm.dll => 105
	i64 u0xccf25c4b634ccd3a, ; 403: zh-Hans/Microsoft.Maui.Controls.resources.dll => 33
	i64 u0xcd10a42808629144, ; 404: System.Net.Requests => 138
	i64 u0xcdd0c48b6937b21c, ; 405: Xamarin.AndroidX.SwipeRefreshLayout => 98
	i64 u0xcf23d8093f3ceadf, ; 406: System.Diagnostics.DiagnosticSource.dll => 121
	i64 u0xd1194e1d8a8de83c, ; 407: lib_Xamarin.AndroidX.Lifecycle.Common.Jvm.dll.so => 87
	i64 u0xd16fd7fb9bbcd43e, ; 408: Microsoft.Extensions.Diagnostics.Abstractions => 53
	i64 u0xd2505d8abeed6983, ; 409: lib_Microsoft.AspNetCore.Components.Web.dll.so => 38
	i64 u0xd333d0af9e423810, ; 410: System.Runtime.InteropServices => 144
	i64 u0xd3426d966bb704f5, ; 411: Xamarin.AndroidX.AppCompat.AppCompatResources.dll => 78
	i64 u0xd3651b6fc3125825, ; 412: System.Private.Uri.dll => 141
	i64 u0xd373685349b1fe8b, ; 413: Microsoft.Extensions.Logging.dll => 61
	i64 u0xd3e4c8d6a2d5d470, ; 414: it/Microsoft.Maui.Controls.resources => 15
	i64 u0xd42655883bb8c19f, ; 415: Microsoft.EntityFrameworkCore.Abstractions.dll => 43
	i64 u0xd4645626dffec99d, ; 416: lib_Microsoft.Extensions.DependencyInjection.Abstractions.dll.so => 51
	i64 u0xd46b4a8758d1f3ee, ; 417: Microsoft.Extensions.FileProviders.Composite.dll => 55
	i64 u0xd6d21782156bc35b, ; 418: Xamarin.AndroidX.SwipeRefreshLayout.dll => 98
	i64 u0xd72329819cbbbc44, ; 419: lib_Microsoft.Extensions.Configuration.Abstractions.dll.so => 49
	i64 u0xd7b3764ada9d341d, ; 420: lib_Microsoft.Extensions.Logging.Abstractions.dll.so => 62
	i64 u0xd7f0088bc5ad71f2, ; 421: Xamarin.AndroidX.VersionedParcelable => 99
	i64 u0xda1dfa4c534a9251, ; 422: Microsoft.Extensions.DependencyInjection => 50
	i64 u0xdad05a11827959a3, ; 423: System.Collections.NonGeneric.dll => 112
	i64 u0xdb5383ab5865c007, ; 424: lib-vi-Microsoft.Maui.Controls.resources.dll.so => 31
	i64 u0xdbeda89f832aa805, ; 425: vi/Microsoft.Maui.Controls.resources.dll => 31
	i64 u0xdbf2a779fbc3ac31, ; 426: System.Transactions.Local.dll => 155
	i64 u0xdbf9607a441b4505, ; 427: System.Linq => 133
	i64 u0xdc75032002d1a212, ; 428: lib_System.Transactions.Local.dll.so => 155
	i64 u0xdca8be7403f92d4f, ; 429: lib_System.Linq.Queryable.dll.so => 132
	i64 u0xdce2c53525640bf3, ; 430: Microsoft.Extensions.Logging => 61
	i64 u0xdd2b722d78ef5f43, ; 431: System.Runtime.dll => 148
	i64 u0xdd67031857c72f96, ; 432: lib_System.Text.Encodings.Web.dll.so => 150
	i64 u0xdde30e6b77aa6f6c, ; 433: lib-zh-Hans-Microsoft.Maui.Controls.resources.dll.so => 33
	i64 u0xde01c9d672ab8d37, ; 434: lib_FokusApp.Presentation.dll.so => 108
	i64 u0xde8769ebda7d8647, ; 435: hr/Microsoft.Maui.Controls.resources.dll => 12
	i64 u0xe0142572c095a480, ; 436: Xamarin.AndroidX.AppCompat.dll => 77
	i64 u0xe02f89350ec78051, ; 437: Xamarin.AndroidX.CoordinatorLayout.dll => 81
	i64 u0xe192a588d4410686, ; 438: lib_System.IO.Pipelines.dll.so => 130
	i64 u0xe1a08bd3fa539e0d, ; 439: System.Runtime.Loader => 145
	i64 u0xe24095a7afddaab3, ; 440: lib_Microsoft.Extensions.Hosting.Abstractions.dll.so => 58
	i64 u0xe2420585aeceb728, ; 441: System.Net.Requests.dll => 138
	i64 u0xe27532f50ce5b0b1, ; 442: Microsoft.Extensions.Localization.dll => 59
	i64 u0xe29b73bc11392966, ; 443: lib-id-Microsoft.Maui.Controls.resources.dll.so => 14
	i64 u0xe31089e70e4e84ee, ; 444: Microsoft.AspNetCore.Components.WebView.Maui => 40
	i64 u0xe332bacb3eb4a806, ; 445: Mono.Android.Export.dll => 161
	i64 u0xe3811d68d4fe8463, ; 446: pt-BR/Microsoft.Maui.Controls.resources.dll => 22
	i64 u0xe494f7ced4ecd10a, ; 447: hu/Microsoft.Maui.Controls.resources.dll => 13
	i64 u0xe4a9b1e40d1e8917, ; 448: lib-fi-Microsoft.Maui.Controls.resources.dll.so => 8
	i64 u0xe5434e8a119ceb69, ; 449: lib_Mono.Android.dll.so => 163
	i64 u0xe5b9c3bba59849e9, ; 450: FokusApp.Core.dll => 106
	i64 u0xe89a2a9ef110899b, ; 451: System.Drawing.dll => 125
	i64 u0xe9772100456fb4b4, ; 452: Microsoft.AspNetCore.Components.dll => 37
	i64 u0xeaa4fe436a4db9bb, ; 453: FokusApp.Core => 106
	i64 u0xed19c616b3fcb7eb, ; 454: Xamarin.AndroidX.VersionedParcelable.dll => 99
	i64 u0xedc632067fb20ff3, ; 455: System.Memory.dll => 134
	i64 u0xee25c2570ce19192, ; 456: lib_Microsoft.Extensions.Localization.Abstractions.dll.so => 60
	i64 u0xeeb7ebb80150501b, ; 457: lib_Xamarin.AndroidX.Collection.Jvm.dll.so => 80
	i64 u0xef72742e1bcca27a, ; 458: Microsoft.Maui.Essentials.dll => 69
	i64 u0xefec0b7fdc57ec42, ; 459: Xamarin.AndroidX.Activity => 76
	i64 u0xf00c29406ea45e19, ; 460: es/Microsoft.Maui.Controls.resources.dll => 7
	i64 u0xf11b621fc87b983f, ; 461: Microsoft.Maui.Controls.Xaml.dll => 67
	i64 u0xf1c4b4005493d871, ; 462: System.Formats.Asn1.dll => 126
	i64 u0xf22514cfad2d598b, ; 463: lib_Xamarin.AndroidX.Lifecycle.ViewModelSavedState.Android.dll.so => 90
	i64 u0xf238bd79489d3a96, ; 464: lib-nl-Microsoft.Maui.Controls.resources.dll.so => 20
	i64 u0xf37221fda4ef8830, ; 465: lib_Xamarin.Google.Android.Material.dll.so => 102
	i64 u0xf3ddfe05336abf29, ; 466: System => 157
	i64 u0xf4103170a1de5bd0, ; 467: System.Linq.Queryable.dll => 132
	i64 u0xf437b94630896f3f, ; 468: lib_Plugin.Maui.Audio.dll.so => 71
	i64 u0xf4c1dd70a5496a17, ; 469: System.IO.Compression => 128
	i64 u0xf6077741019d7428, ; 470: Xamarin.AndroidX.CoordinatorLayout => 81
	i64 u0xf77b20923f07c667, ; 471: de/Microsoft.Maui.Controls.resources.dll => 5
	i64 u0xf7e2cac4c45067b3, ; 472: lib_System.Numerics.Vectors.dll.so => 139
	i64 u0xf7e74930e0e3d214, ; 473: zh-HK/Microsoft.Maui.Controls.resources.dll => 32
	i64 u0xf84773b5c81e3cef, ; 474: lib-uk-Microsoft.Maui.Controls.resources.dll.so => 30
	i64 u0xf8aac5ea82de1348, ; 475: System.Linq.Queryable => 132
	i64 u0xf8e045dc345b2ea3, ; 476: lib_Xamarin.AndroidX.RecyclerView.dll.so => 96
	i64 u0xf91db4f1ad5d43b1, ; 477: Plugin.Maui.Audio.dll => 71
	i64 u0xf96c777a2a0686f4, ; 478: hi/Microsoft.Maui.Controls.resources.dll => 11
	i64 u0xf9eec5bb3a6aedc6, ; 479: Microsoft.Extensions.Options => 63
	i64 u0xfa504dfa0f097d72, ; 480: Microsoft.Extensions.FileProviders.Abstractions.dll => 54
	i64 u0xfa5ed7226d978949, ; 481: lib-ar-Microsoft.Maui.Controls.resources.dll.so => 1
	i64 u0xfa645d91e9fc4cba, ; 482: System.Threading.Thread => 153
	i64 u0xfb022853d73b7fa5, ; 483: lib_SQLitePCLRaw.batteries_v2.dll.so => 72
	i64 u0xfbf0a31c9fc34bc4, ; 484: lib_System.Net.Http.dll.so => 135
	i64 u0xfc6b7527cc280b3f, ; 485: lib_System.Runtime.Serialization.Formatters.dll.so => 147
	i64 u0xfc719aec26adf9d9, ; 486: Xamarin.AndroidX.Navigation.Fragment.dll => 93
	i64 u0xfd22f00870e40ae0, ; 487: lib_Xamarin.AndroidX.DrawerLayout.dll.so => 85
	i64 u0xfd2e866c678cac90, ; 488: lib_Microsoft.AspNetCore.Components.WebView.Maui.dll.so => 40
	i64 u0xfd49b3c1a76e2748, ; 489: System.Runtime.InteropServices.RuntimeInformation => 143
	i64 u0xfd583f7657b6a1cb, ; 490: Xamarin.AndroidX.Fragment => 86
	i64 u0xfeae9952cf03b8cb ; 491: tr/Microsoft.Maui.Controls.resources => 29
], align 16

@assembly_image_cache_indices = dso_local local_unnamed_addr constant [492 x i32] [
	i32 98, i32 115, i32 47, i32 162, i32 77, i32 75, i32 25, i32 3,
	i32 31, i32 137, i32 96, i32 41, i32 114, i32 68, i32 41, i32 32,
	i32 80, i32 37, i32 25, i32 112, i32 0, i32 85, i32 115, i32 63,
	i32 112, i32 149, i32 46, i32 26, i32 105, i32 100, i32 22, i32 163,
	i32 69, i32 84, i32 107, i32 127, i32 96, i32 9, i32 160, i32 10,
	i32 51, i32 52, i32 158, i32 13, i32 150, i32 105, i32 19, i32 110,
	i32 157, i32 28, i32 56, i32 162, i32 95, i32 17, i32 63, i32 127,
	i32 148, i32 28, i32 107, i32 153, i32 42, i32 119, i32 82, i32 9,
	i32 72, i32 103, i32 64, i32 14, i32 12, i32 160, i32 137, i32 53,
	i32 92, i32 30, i32 136, i32 122, i32 8, i32 152, i32 126, i32 34,
	i32 57, i32 21, i32 154, i32 27, i32 151, i32 6, i32 65, i32 156,
	i32 123, i32 46, i32 86, i32 35, i32 79, i32 124, i32 9, i32 156,
	i32 57, i32 111, i32 7, i32 55, i32 68, i32 3, i32 66, i32 115,
	i32 101, i32 48, i32 111, i32 84, i32 100, i32 2, i32 54, i32 65,
	i32 97, i32 103, i32 107, i32 82, i32 72, i32 39, i32 58, i32 78,
	i32 59, i32 158, i32 163, i32 21, i32 109, i32 103, i32 43, i32 122,
	i32 52, i32 25, i32 23, i32 140, i32 60, i32 99, i32 95, i32 151,
	i32 90, i32 44, i32 91, i32 136, i32 131, i32 142, i32 145, i32 15,
	i32 91, i32 65, i32 162, i32 2, i32 74, i32 66, i32 108, i32 36,
	i32 89, i32 125, i32 137, i32 120, i32 82, i32 70, i32 26, i32 136,
	i32 143, i32 32, i32 148, i32 92, i32 87, i32 113, i32 141, i32 73,
	i32 159, i32 58, i32 121, i32 16, i32 50, i32 109, i32 81, i32 154,
	i32 118, i32 4, i32 61, i32 40, i32 144, i32 80, i32 113, i32 150,
	i32 116, i32 156, i32 120, i32 74, i32 6, i32 50, i32 104, i32 134,
	i32 67, i32 5, i32 145, i32 159, i32 94, i32 111, i32 102, i32 66,
	i32 146, i32 119, i32 89, i32 83, i32 4, i32 124, i32 126, i32 10,
	i32 74, i32 144, i32 19, i32 59, i32 70, i32 64, i32 83, i32 64,
	i32 93, i32 68, i32 3, i32 129, i32 29, i32 19, i32 53, i32 15,
	i32 116, i32 12, i32 134, i32 45, i32 48, i32 146, i32 18, i32 28,
	i32 86, i32 39, i32 8, i32 41, i32 117, i32 26, i32 5, i32 92,
	i32 18, i32 139, i32 114, i32 123, i32 140, i32 118, i32 100, i32 49,
	i32 88, i32 71, i32 75, i32 38, i32 157, i32 60, i32 0, i32 34,
	i32 77, i32 79, i32 125, i32 30, i32 33, i32 55, i32 44, i32 108,
	i32 34, i32 48, i32 57, i32 43, i32 153, i32 127, i32 69, i32 104,
	i32 158, i32 116, i32 97, i32 143, i32 46, i32 91, i32 121, i32 10,
	i32 54, i32 83, i32 45, i32 154, i32 110, i32 11, i32 24, i32 73,
	i32 23, i32 22, i32 73, i32 122, i32 35, i32 128, i32 89, i32 67,
	i32 84, i32 151, i32 133, i32 2, i32 18, i32 128, i32 0, i32 7,
	i32 14, i32 70, i32 118, i32 110, i32 97, i32 131, i32 36, i32 17,
	i32 155, i32 106, i32 76, i32 49, i32 94, i32 20, i32 88, i32 56,
	i32 149, i32 102, i32 95, i32 130, i32 17, i32 36, i32 120, i32 147,
	i32 129, i32 139, i32 149, i32 85, i32 87, i32 13, i32 161, i32 56,
	i32 62, i32 142, i32 135, i32 51, i32 6, i32 131, i32 146, i32 88,
	i32 24, i32 20, i32 117, i32 160, i32 140, i32 45, i32 27, i32 123,
	i32 37, i32 152, i32 4, i32 79, i32 11, i32 1, i32 130, i32 62,
	i32 124, i32 27, i32 159, i32 90, i32 23, i32 16, i32 114, i32 39,
	i32 147, i32 75, i32 135, i32 101, i32 78, i32 1, i32 119, i32 133,
	i32 42, i32 76, i32 16, i32 161, i32 129, i32 101, i32 52, i32 109,
	i32 42, i32 47, i32 117, i32 138, i32 93, i32 142, i32 141, i32 94,
	i32 113, i32 38, i32 47, i32 29, i32 21, i32 24, i32 44, i32 35,
	i32 152, i32 104, i32 105, i32 33, i32 138, i32 98, i32 121, i32 87,
	i32 53, i32 38, i32 144, i32 78, i32 141, i32 61, i32 15, i32 43,
	i32 51, i32 55, i32 98, i32 49, i32 62, i32 99, i32 50, i32 112,
	i32 31, i32 31, i32 155, i32 133, i32 155, i32 132, i32 61, i32 148,
	i32 150, i32 33, i32 108, i32 12, i32 77, i32 81, i32 130, i32 145,
	i32 58, i32 138, i32 59, i32 14, i32 40, i32 161, i32 22, i32 13,
	i32 8, i32 163, i32 106, i32 125, i32 37, i32 106, i32 99, i32 134,
	i32 60, i32 80, i32 69, i32 76, i32 7, i32 67, i32 126, i32 90,
	i32 20, i32 102, i32 157, i32 132, i32 71, i32 128, i32 81, i32 5,
	i32 139, i32 32, i32 30, i32 132, i32 96, i32 71, i32 11, i32 63,
	i32 54, i32 1, i32 153, i32 72, i32 135, i32 147, i32 93, i32 85,
	i32 40, i32 143, i32 86, i32 29
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

; Function attributes: memory(write, argmem: none, inaccessiblemem: none) "min-legal-vector-width"="0" mustprogress "no-trapping-math"="true" nofree norecurse nosync nounwind "stack-protector-buffer-size"="8" uwtable willreturn
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

; Function attributes: "no-trapping-math"="true" noreturn nounwind "stack-protector-buffer-size"="8"
declare void @abort() local_unnamed_addr #2

; Function attributes: nofree nounwind
declare noundef i32 @puts(ptr noundef) local_unnamed_addr #1
attributes #0 = { memory(write, argmem: none, inaccessiblemem: none) "min-legal-vector-width"="0" mustprogress "no-trapping-math"="true" nofree norecurse nosync nounwind "stack-protector-buffer-size"="8" "target-cpu"="x86-64" "target-features"="+crc32,+cx16,+cx8,+fxsr,+mmx,+popcnt,+sse,+sse2,+sse3,+sse4.1,+sse4.2,+ssse3,+x87" "tune-cpu"="generic" uwtable willreturn }
attributes #1 = { nofree nounwind }
attributes #2 = { "no-trapping-math"="true" noreturn nounwind "stack-protector-buffer-size"="8" "target-cpu"="x86-64" "target-features"="+crc32,+cx16,+cx8,+fxsr,+mmx,+popcnt,+sse,+sse2,+sse3,+sse4.1,+sse4.2,+ssse3,+x87" "tune-cpu"="generic" }

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
