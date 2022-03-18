using System;
using System.Collections.Generic;
using Perforce.P4;

namespace P4Nuker
{
    class Program
    {
        static void Main(string[] args)
        {
            // initialize the connection variables
            // note: this is a connection without using a password
            string uri = "ServerAdressHere";
            string user = "UserNameHere";
            string ws_client = "P4ClientNameHere";


            // define the server, repository and connection
            Server server = new Server(new ServerAddress(uri));
            Repository rep = new Repository(server);
            Connection con = rep.Connection;


            // use the connection variables for this connection
            con.UserName = user;
            con.Client = new Client();
            con.Client.Name = ws_client;
			con.CurrentWorkingDirectory = "C:\\Perforce\\Main";


            // connect to the server
            con.Connect(null);

			List<FileSpec> filesToCheckOut = new List<FileSpec>();
			List<FileSpec> fileSpecs = new List<FileSpec>();
			fileSpecs.Add(new DepotPath("//depot/main/XXX/Content/WwiseData/....uasset"));
			
			GetDepotFilesCmdOptions opts = new GetDepotFilesCmdOptions(GetDepotFilesCmdFlags.NotDeleted, 0);

			IList<FileSpec> filespecs = rep.GetDepotFiles(fileSpecs, opts);

			IList<File> files = rep.GetFiles(filespecs, null);

			//const int limitForExperimentation = 5;
			foreach (File file in files)
			{
				if (file.Type.StoredRevs == 0)
				{
					FileSpec fileToCheckout = new FileSpec(new DepotPath(file.DepotPath.ToString()), null, null, null);
					filesToCheckOut.Add(fileToCheckout);
				}

				//if (filesToCheckOut.Count >= limitForExperimentation)
				//{
				//	break;
				//}
			}

			Changelist cl = new Changelist();
			cl.Description = "Limiting revisions of WwiseData";
			cl = rep.CreateChangelist(cl, new Options());

			EditCmdOptions options = new EditCmdOptions(EditFilesCmdFlags.None, cl.Id, new FileType(BaseFileType.Binary, FileTypeModifier.HeadrevOnly));

			IList <FileSpec> checkedOutFiles = con.Client.EditFiles(filesToCheckOut, options);

			// Obliterate old revisions of checked out files. We don't want to submit in case this goes wrong, so that we can always get all depot files that keeps more than 1 rev.

			foreach (FileSpec filespec in checkedOutFiles)
			{
				string version = filespec.Version.ToString();
				int versionnumber = int.Parse(version.Substring(1));

				if (versionnumber > 1)
				{
					string argument = filespec.DepotPath.ToString() + "#1," + (versionnumber - 1);
					P4Command cmd = new P4Command(rep, "obliterate", true, argument);
					Options opts2 = new Options();
					opts2["-y"] = "";

					P4CommandResult results = cmd.Run(opts2);
				}
			}

			cl.Submit(null);
		}
    }
}
