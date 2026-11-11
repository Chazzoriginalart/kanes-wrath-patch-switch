using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using System.Security.Cryptography;
class ReleaseInstall {
 static string Optional4K(string game,string text){
  if(File.Exists(Path.Combine(game,"4KMod","config.txt")))return text;
  return System.Text.RegularExpressions.Regex.Replace(text,@"(?im)^add-config(?=[ 	]+4kmod[/\x5c]config\.txt[ 	]*\r?$)","try-add-config");
 }
 static string Hash(string p){using(var s=File.OpenRead(p))using(var h=SHA256.Create())return BitConverter.ToString(h.ComputeHash(s)).Replace("-","");}
 static string Dest(string game,string maps,string rel){return rel.StartsWith("CustomMaps\\",StringComparison.OrdinalIgnoreCase)?Path.Combine(maps,rel.Substring(11)):Path.Combine(game,rel);}
 static int Main(string[] args){
  string log=args.Length==4?args[3]:null;
  try{
   if(args.Length!=4)throw new Exception("Invalid installation parameters.");
   string payload=Path.GetFullPath(args[0]),game=Path.GetFullPath(args[1]),maps=Path.GetFullPath(args[2]);
   foreach(var p in System.Diagnostics.Process.GetProcesses())if(new List<string>{"cnc3ep1","cnc3ep1.dat","worldbuilder"}.Contains(p.ProcessName.ToLowerInvariant()))throw new Exception("Close Kane's Wrath and World Builder first.");
   if(!File.Exists(Path.Combine(game,"CNC3EP1.exe")) || !Directory.Exists(Path.Combine(game,"Core")))throw new Exception("Select the game installation folder containing CNC3EP1.exe.");
   var configs=Directory.GetFiles(game,"CNC3EP1_*_1.2.SkuDef");if(configs.Length==0)throw new Exception("Kane's Wrath 1.02 configuration files were not found.");
   var expected=new Dictionary<string,string>(StringComparer.OrdinalIgnoreCase);
   foreach(string line in File.ReadAllLines(Path.Combine(payload,"payload.sha256"))){
    string[] s=line.Split('|');if(s.Length!=2 || Path.IsPathRooted(s[1]) || s[1].Contains("..") || s[1].Contains(":"))throw new Exception("Invalid manifest.");
    string rel=s[1].Replace('/','\\');if(!(rel.StartsWith("Patch103\\") || rel.StartsWith("CustomMaps\\")) || expected.ContainsKey(rel))throw new Exception("Invalid payload path.");
    if(!Hash(Path.Combine(payload,rel)).Equals(s[0],StringComparison.OrdinalIgnoreCase))throw new Exception("Payload mismatch: "+rel);expected.Add(rel,s[0]);
   }
   if(expected.Count!=26)throw new Exception("Incomplete release payload.");
   foreach(string root in new[]{game,maps}){
    for(var p=new DirectoryInfo(root);p!=null;p=p.Parent)if(p.Exists && (p.Attributes&FileAttributes.ReparsePoint)!=0)throw new Exception("Linked installation folder is not supported: "+p.FullName);
   }
   foreach(string rel in expected.Keys){string target=Dest(game,maps,rel);for(var p=new DirectoryInfo(Path.GetDirectoryName(target));p!=null;p=p.Parent)if(p.Exists && (p.Attributes&FileAttributes.ReparsePoint)!=0)throw new Exception("Linked target folder is not supported: "+p.FullName);if(File.Exists(target) && (File.GetAttributes(target)&FileAttributes.ReparsePoint)!=0)throw new Exception("Linked target file is not supported: "+target);}
   var edits=new Dictionary<string,string>();
   foreach(string cfg in Directory.GetFiles(game,"*.SkuDef")){
    string text=File.ReadAllText(cfg);bool found=false;
    foreach(string line in text.Split('\n'))if(line.Trim().Replace('/','\\').Equals("add-config Patch103\\config.txt",StringComparison.OrdinalIgnoreCase))found=true;
    if(!found && cfg.EndsWith("_1.2.SkuDef",StringComparison.OrdinalIgnoreCase)){var match=System.Text.RegularExpressions.Regex.Match(text,@"(?im)^[ \t]*add-config[ \t]+");int at=match.Success?match.Index:-1;text=at<0?text.TrimEnd()+"\r\nadd-config Patch103\\config.txt\r\n":text.Insert(at,"add-config Patch103\\config.txt\r\n");}
    string updated=Optional4K(game,text);if(updated!=File.ReadAllText(cfg))edits.Add(Path.GetFileName(cfg),updated);
   }
   string wb=Path.Combine(game,"CNC3_wb_1.0.cfg");
   if(File.Exists(wb)){string original=File.ReadAllText(wb);string updated=Optional4K(game,original);if(updated!=original)edits.Add(Path.GetFileName(wb),updated);}
   edits.Add("KaneWrathPatchSwitch-R25j-R3-installed.txt","KaneWrathPatchSwitch-R25j-R3 - LOCAL RELEASE\r\nRepaired R25j Fixes R2 core with four updated custom maps and optional 4K configuration.\r\nRelease label only; the game's displayed version number is unchanged.\r\nExtra mods, launcher settings and runtime multiplayer parity are not certified.\r\n");
   var all=new List<string>(expected.Keys);all.AddRange(edits.Keys);
   string backup=Path.Combine(game,"KaneWrathPatchSwitch-R25j-R3-Backups",DateTime.Now.ToString("yyyyMMdd-HHmmss")+"-"+Guid.NewGuid().ToString("N").Substring(0,6));Directory.CreateDirectory(backup);
   var existed=new HashSet<string>();
   foreach(string rel in all){string p=Dest(game,maps,rel);if(File.Exists(p)){string saved=Path.Combine(backup,rel);Directory.CreateDirectory(Path.GetDirectoryName(saved));File.Copy(p,saved);if(Hash(p)!=Hash(saved))throw new Exception("Backup mismatch: "+rel);existed.Add(rel);}}
   File.WriteAllLines(Path.Combine(backup,"destinations.txt"),all.ConvertAll(x=>x+"|"+Dest(game,maps,x)).ToArray());
   File.WriteAllLines(Path.Combine(backup,"absent.txt"),all.FindAll(x=>!existed.Contains(x)).ToArray());
   var touched=new List<string>();
   try{
    foreach(string rel in all){string p=Dest(game,maps,rel);Directory.CreateDirectory(Path.GetDirectoryName(p));touched.Add(rel);
     if(edits.ContainsKey(rel)){File.WriteAllText(p,edits[rel],Encoding.ASCII);if(File.ReadAllText(p)!=edits[rel])throw new Exception("Configuration verification failed: "+rel);}
     else{File.Copy(Path.Combine(payload,rel),p,true);if(!Hash(p).Equals(expected[rel],StringComparison.OrdinalIgnoreCase))throw new Exception("Installed mismatch: "+rel);}
    }
   }catch(Exception failure){var errors=new List<string>();foreach(string rel in touched)try{string p=Dest(game,maps,rel);if(existed.Contains(rel))File.Copy(Path.Combine(backup,rel),p,true);else if(File.Exists(p))File.Delete(p);}catch(Exception e){errors.Add(e.Message);}throw new Exception(failure.Message+" Backup: "+backup+". "+(errors.Count==0?"Original files restored.":"Rollback needs attention: "+String.Join("; ",errors.ToArray())));}
   File.WriteAllText(log,"KaneWrathPatchSwitch-R25j-R3 installed and verified. Backups: "+backup,Encoding.UTF8);return 0;
  }catch(Exception e){if(log!=null)File.WriteAllText(log,e.Message,Encoding.UTF8);return 1;}
 }
}
