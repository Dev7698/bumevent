using bumevent.Models;
using DocumentFormat.OpenXml.Packaging;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using TemplateEngine.Docx;
using DocumentFormat.OpenXml.Wordprocessing;
using DocumentFormat.OpenXml;
using System;
using System.IO;
using System.Linq;

using DW = DocumentFormat.OpenXml.Drawing.Wordprocessing;
using A = DocumentFormat.OpenXml.Drawing;
using PIC = DocumentFormat.OpenXml.Drawing.Pictures;



namespace bumevent.Controllers
{
    public class EventController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _hostEnvironment;
        private readonly IConfiguration _configuration; // Declare the field

        public EventController(ApplicationDbContext context, IWebHostEnvironment hostEnvironment, IConfiguration configuration = null  )
        {
            _context = context;
            _hostEnvironment = hostEnvironment;
            _configuration = configuration;
        }

        // GET: Event/Index
        public IActionResult Index()
        {
             
            return View();
        }

        // POST: Event/Index
        [HttpPost]
        public async Task<IActionResult> Index(Event evnt)
        {
            // Remove the ModelState entry for ImagePath so it won't block validation.
            ModelState.Remove("ImagePath");

            if (ModelState.IsValid)
            {
                // Handle file upload
                if (Request.Form.Files.Count > 0)
                {
                    var file = Request.Form.Files[0];
                    string imagesFolder = Path.Combine(_hostEnvironment.WebRootPath, "images");
                    if (!Directory.Exists(imagesFolder))
                        Directory.CreateDirectory(imagesFolder);
                    string uniqueFileName = Guid.NewGuid().ToString() + "_" + file.FileName;
                    string filePath = Path.Combine(imagesFolder, uniqueFileName);
                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await file.CopyToAsync(stream);
                    }
                    evnt.ImagePath = "/images/" + uniqueFileName;
                }
                else
                {
                    // If no file was uploaded, set a default image
                    string defaultImagePath = "/images/bmu.png";
                    string imagesFolder = Path.Combine(_hostEnvironment.WebRootPath, "images");

                    // Check if the default image exists in the folder, if not, copy it from a source location
                    string defaultImageSourcePath = Path.Combine(_hostEnvironment.WebRootPath, "default_images", "bmu.png");
                    string destinationImagePath = Path.Combine(imagesFolder, "bmu.png");

                    if (!Directory.Exists(imagesFolder))
                        Directory.CreateDirectory(imagesFolder);

                    if (!System.IO.File.Exists(destinationImagePath))
                    {
                        
                        System.IO.File.Copy(defaultImageSourcePath, destinationImagePath);
                    }

                    evnt.ImagePath = "/images/bmu.png";
                }

                _context.Events.Add(evnt);
                await _context.SaveChangesAsync();
                return RedirectToAction("Details", new { id = evnt.Id });
            }
            return View(evnt);
        }



        // GET: Event/Details/5
        // In your EventController.cs
        public async Task<IActionResult> Details(int? id)
        {
            if (id.HasValue)
            {
                // Show single event details
                var evnt = await _context.Events.FindAsync(id);
                if (evnt == null) return NotFound();
                return View("Details", evnt);
            }

            // Show all events list
            var allEvents = await _context.Events.ToListAsync();
            return View("eventlist", allEvents);
        }
        
        public IActionResult DownloadDocument(int id) 
        {
            string connectionString = _configuration.GetConnectionString("DefaultConnection"); Event evnt = null;

        
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();
                string query = @"SELECT [Id]
                                      ,[EventTitle]
                                      ,[EventDate]
                                      ,[EventPlace]
                                      ,[CoordinatorName]
                                      ,[StudentCount]
                                      ,[DepartmentName]
                                      ,[Objective]
                                      ,[ImagePath]
                                 FROM [devpatel_].[dbo].[Events] 
                                WHERE Id = @id"; 
                using (SqlCommand cmd = new SqlCommand(query, connection)) 
                {
                    cmd.Parameters.AddWithValue("@id", id); 
                    using (SqlDataReader reader = cmd.ExecuteReader()) 
                    {
                        if (reader.Read()) 
                        { 
                            evnt = new Event() 
                            {
                                Id = reader.GetInt32(0),
                                EventTitle = reader.GetString(1),
                                EventDate = reader.GetDateTime(2),
                                EventPlace = reader.GetString(3),
                                CoordinatorName = reader.GetString(4),
                                StudentCount = reader.GetInt32(5),
                                DepartmentName = reader.GetString(6),
                                Objective = reader.GetString(7),
                                ImagePath = reader.IsDBNull(8) ? null : reader.GetString(8)
                            };
                        }
                    }
                }
            }

            if (evnt == null)
            {
                return NotFound();
            }

             string templatePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "Templates", "eventtemplate.docx");

            byte[] fileBytes = System.IO.File.ReadAllBytes(templatePath);
            using (MemoryStream memStream = new MemoryStream())
            {
                memStream.Write(fileBytes, 0, fileBytes.Length);

                 using (WordprocessingDocument wordDoc = WordprocessingDocument.Open(memStream, true))
                {
                    // Replace placeholders with event details
                    ReplacePlaceholder(wordDoc, "{{EventTitle}}", evnt.EventTitle);
                    ReplacePlaceholder(wordDoc, "{{EventDate}}", evnt.EventDate.ToShortDateString());
                    ReplacePlaceholder(wordDoc, "{{EventPlace}}", evnt.EventPlace);
                    ReplacePlaceholder(wordDoc, "{{CoordinatorName}}", evnt.CoordinatorName);
                    ReplacePlaceholder(wordDoc, "{{StudentCount}}", evnt.StudentCount.ToString());
                    ReplacePlaceholder(wordDoc, "{{DepartmentName}}", evnt.DepartmentName);
                    ReplacePlaceholder(wordDoc, "{{Objective}}", evnt.Objective);
                     if (!string.IsNullOrEmpty(evnt.ImagePath))
                    {
                         string absoluteImagePath = Path.Combine(_hostEnvironment.WebRootPath, evnt.ImagePath.TrimStart('/'));
                        if (System.IO.File.Exists(absoluteImagePath))
                        {
                            InsertImage(wordDoc, "{{EventImage}}", absoluteImagePath);
                        }
                    }
                     wordDoc.MainDocumentPart.Document.Save();
                }

                // Return the modified document as a downloadable file.
                return File(memStream.ToArray(),
                            "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                            "EventDocument.docx");
            }
        }

         private void ReplacePlaceholder(WordprocessingDocument doc, string placeholder, string newValue)
        {
            foreach (var text in doc.MainDocumentPart.Document.Descendants<Text>())
            {
                if (text.Text.Contains(placeholder)) 
                {
                    text.Text = text.Text.Replace(placeholder, newValue);
                }
            }
        }
        private string GetContentType(string imagePath)
        {
            string ext = Path.GetExtension(imagePath).ToLower();
            switch (ext)
            {
                case ".png":
                    return "image/png";
                case ".jpg":
                case ".jpeg":
                    return "image/jpeg";
                case ".gif":
                    return "image/gif";
                default:
                    throw new InvalidOperationException("Unsupported image type: " + ext);
            }
        }

        private void InsertImage(WordprocessingDocument wordDoc, string placeholder, string imagePath)
        {
            // Find the paragraph that contains the placeholder text.
            var body = wordDoc.MainDocumentPart.Document.Body;
            var placeholderParagraph = body.Descendants<Paragraph>()
                .FirstOrDefault(p => p.InnerText.Contains(placeholder));

            if (placeholderParagraph == null)
            {
                // Optionally log or handle if placeholder is not found.
                return;
            }

             placeholderParagraph.RemoveAllChildren<Run>();

             byte[] imageBytes = System.IO.File.ReadAllBytes(imagePath); 

             string contentType = GetContentType(imagePath);

            ImagePart imagePart = wordDoc.MainDocumentPart.AddNewPart<ImagePart>(contentType);
            using (var stream = new MemoryStream(imageBytes))
            {
                imagePart.FeedData(stream);
            }

             string relationshipId = wordDoc.MainDocumentPart.GetIdOfPart(imagePart);

            // Create the drawing element.
            var drawingElement = GetImageDrawingElement(relationshipId);

             placeholderParagraph.AppendChild(new Run(drawingElement));
        }


        private Drawing GetImageDrawingElement(string relationshipId)
        {
             long widthEmus = 990000L;  // approx. 1.08 inches wide (adjust as needed)
            long heightEmus = 792000L; // approx. 0.87 inches high (adjust as needed)

            return new Drawing(
                new DW.Inline(
                    new DW.Extent() { Cx = widthEmus, Cy = heightEmus },
                    new DW.EffectExtent()
                    {
                        LeftEdge = 0L,
                        TopEdge = 0L,
                        RightEdge = 0L,
                        BottomEdge = 0L
                    },
                    new DW.DocProperties()
                    {
                        Id = (UInt32Value)1U,
                        Name = "Event Image"
                    },
                    new DW.NonVisualGraphicFrameDrawingProperties(
                        new A.GraphicFrameLocks() { NoChangeAspect = true }
                    ),
                    new A.Graphic(
                        new A.GraphicData(
                            new PIC.Picture(
                                new PIC.NonVisualPictureProperties(
                                    new PIC.NonVisualDrawingProperties()
                                    {
                                        Id = (UInt32Value)0U,
                                        Name = "Inserted Image"
                                    },
                                    new PIC.NonVisualPictureDrawingProperties()
                                ),
                                new PIC.BlipFill(
                                    new A.Blip() { Embed = relationshipId, CompressionState = A.BlipCompressionValues.Print },
                                    new A.Stretch(new A.FillRectangle())
                                ),
                                new PIC.ShapeProperties(
                                    new A.Transform2D(
                                        new A.Offset() { X = 0L, Y = 0L },
                                        new A.Extents() { Cx = widthEmus, Cy = heightEmus }
                                    ),
                                    new A.PresetGeometry(new A.AdjustValueList())
                                    { Preset = A.ShapeTypeValues.Rectangle }
                                )
                            )
                        )
                        { Uri = "http://schemas.openxmlformats.org/drawingml/2006/picture" }
                    )
                )
                {
                    DistanceFromTop = (UInt32Value)0U,
                    DistanceFromBottom = (UInt32Value)0U,
                    DistanceFromLeft = (UInt32Value)0U,
                    DistanceFromRight = (UInt32Value)0U
                });
        }

    }
}