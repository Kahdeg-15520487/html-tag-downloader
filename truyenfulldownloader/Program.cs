using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Net;
using System.Threading;
using System.Reflection;

using HtmlAgilityPack;

using Newtonsoft.Json;

using Konsole;

using Utility.CommandLine;

namespace truyenfulldownloader
{
    class Program
    {
        static readonly string AssemblyLocation = Path.GetDirectoryName(Assembly.GetAssembly(typeof(Program)).Location);

        const string TruyenFullTag = "";
        const string Header = @"<!DOCTYPE html><html lang=""vi""><head><link rel=""stylesheet"" type=""text/css"" href=""style.css""/></head><body class=""bodynight"" id=""body""><div class=""contentnight"" id=""content""><script src=""function.js""></script><button onclick=""changeColor()"" id=""button_change_color"">day</button><br/>";
        const string Footer = @"</div></body></html>";
        const string Link = @"<a href=""!!!!"">!!!!</a><br>";
        const string Navigation = @"<div id=""expand-box-header""><span style=""float: left;"">!!!!</span><span style=""float: right;"">????</span><div style=""clear:both;""></div></div>";

        [Argument('f', "from")]
        static string From { get; set; } = null;

        [Argument('t', "to")]
        static string To { get; set; } = null;

        [Argument('p', "path")]
        static string XPath { get; set; } = null;

        [Argument('i', "img")]
        static bool Image { get; set; } = false;

        [Operands]
        static string[] Config { get; set; }

        static void Main(string[] args)
        {

            Arguments.Populate();

            if (Config.Length == 1)
            {
                Console.WriteLine("truyenfulldownloader -f \"tu chuong\" -t \"toi chuong\" [-p \"XPath of the html tag\" -i [true|false] ] \"ten file config\"");
                return;
            }

            for (int configfile = 1; configfile < Config.Length; configfile++)
            {
                if (File.Exists(Config[configfile]))
                {

                    //load and parse config file
                    var config = File.ReadAllText(Config[configfile]).Split('|');
                    var name = Path.GetFileNameWithoutExtension(Config[configfile]);
                    var url = config[0];
                    var chapters = JsonConvert.DeserializeObject<string[]>(config[1]);
                    int chapterscount = chapters.Length;
                    int chapternumpadleft = chapterscount.ToString().Length;

                    //create save directory
                    Directory.CreateDirectory(name);
                    File.Copy(Path.Combine(AssemblyLocation, "function.js"), name + "\\function.js", true);
                    File.Copy(Path.Combine(AssemblyLocation, "style.css"), name + "\\style.css", true);

                    var web = new HtmlWeb();
                    HtmlNode.ElementsFlags["br"] = HtmlElementFlag.Empty;

                    //generate progress bar
                    var progressBar = new ProgressBar(chapterscount);
                    progressBar.Refresh(0, string.Format("getting {0} chapter(s)", chapterscount));

                    List<string> failedfile = new List<string>();

                    int cc = 0;

                    if (From != null)
                    {
                        for (; cc < chapters.Length; cc++)
                        {
                            if (chapters[cc] == From)
                            {
                                break;
                            }
                        }
                    }

                    for (; cc < chapterscount; cc++)
                    {
                        Thread.Sleep(2000);

                        // Call the page and get the generated HTML
                        HtmlDocument doc = null;
                        int excount = 0;
                        do
                        {
                            try
                            {
                                excount = 0;
                                doc = web.Load(url + chapters[cc]);
                                doc.OptionWriteEmptyNodes = true;
                            }
                            catch (UriFormatException uex)
                            {
                                Console.WriteLine("{0} : uri format {2} : {1}", excount++, uex.Message, url + chapters[cc]);
                            }
                            catch (WebException wex)
                            {
                                Console.WriteLine("{0} : web {1}", excount++, wex.Message);
                                excount++;
                            }
                        } while (excount != 0);

                        //get the div by id and then get the inner text 
                        string xpath = XPath ?? "//div[@class='chapter-c']";
                        var node = doc.DocumentNode.SelectSingleNode(xpath);

                        if (node != null)
                        {

                            //generate navigation
                            string prev = cc == 0 ? string.Empty : cc.ToString().PadLeft(chapternumpadleft, '0') + "_" + (cc == 0 ? "" : chapters[cc - 1]) + ".html";
                            string next = cc == chapterscount - 1 ? string.Empty : (cc + 2).ToString().PadLeft(chapternumpadleft, '0') + "_" + (cc == chapterscount - 1 ? "" : chapters[cc + 1]) + ".html";
                            string current = (cc + 1).ToString().PadLeft(chapternumpadleft, '0') + "_" + chapters[cc];
                            string linkPrev = Link.Replace("!!!!", prev);
                            string linkNext = Link.Replace("!!!!", next);
                            string navigation = Navigation.Replace("!!!!", linkPrev).Replace("????", linkNext);

                            if (Image)
                            {
                                var testImgSelector = "//img";
                                var imgnodes = node.SelectNodes(testImgSelector);
                                if (imgnodes != null)
                                {
                                    foreach (var imgnode in imgnodes)
                                    {
                                        var imgUrl = imgnode.Attributes["src"].Value;
                                        var imgFileName = Path.GetFileName(imgUrl);
                                        //using (WebClient client = new WebClient())
                                        //{
                                        //    client.DownloadFile(new Uri(imgUrl), Path.Combine(name, imgFileName));
                                        //}
                                        imgnode.Attributes["src"].Value = imgFileName;
                                    }
                                }
                            }

                            var divString = node.InnerHtml;


                            StringBuilder result = new StringBuilder();
                            result.AppendLine(Header);
                            result.AppendLine(divString);
                            result.AppendLine(navigation);

                            result.AppendLine(Footer);

                            //write
                            var tempPath = Path.Combine(name, current);
                            File.WriteAllText(tempPath.Substring(0, Math.Min(tempPath.Length, 150)) + ".html", result.ToString(), Encoding.UTF8);

                            progressBar.Refresh(cc, current);
                        }
                        else
                        {
                            Console.WriteLine("error getting content from {0}", url + chapters[cc]);
                            failedfile.Add(chapters[cc]);
                        }
                    }

                    if (failedfile.Count > 0)
                    {

                        StringBuilder log = new StringBuilder();
                        log.Append(url);
                        log.Append('|');
                        log.Append(JsonConvert.SerializeObject(failedfile.ToArray()));
                        File.WriteAllText($"failedfile_{name}.txt", log.ToString());
                    }
                }
            }
        }
    }
}
