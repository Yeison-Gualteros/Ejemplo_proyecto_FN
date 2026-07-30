using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using Shared.DataTransferObjects.Documentos;
using System.Diagnostics;
using System.Text;
using A = DocumentFormat.OpenXml.Drawing;
using DW = DocumentFormat.OpenXml.Drawing.Wordprocessing;
using PIC = DocumentFormat.OpenXml.Drawing.Pictures;

namespace Service.Ayudante
{
    public static class WordTemplateHelper
    {
        private static readonly string LibreOfficePath =
           @"C:\Program Files\LibreOffice\program\soffice.com";

        // CONFIGURACIÓN DE ESTILOS 
        private static readonly string FuenteNormal = "Ebrima";
        private static readonly string TamanoContenido = "24"; // 12pt = 24 medios puntos
        private static readonly string TamanoFiguras = "16";   // 8pt
        private static readonly string ColorTitulo1 = "000000";
        private static readonly string ColorTitulo2 = "000000";
        private static readonly string ColorTexto = "000000";
        private static readonly string ColorTablaEncabezado = "D9D9D9"; // Gris claro

        // Variable para insertar imágenes
        [ThreadStatic]
        private static MainDocumentPart? _currentMainDocumentPart;

        // MÉTODO PRINCIPAL
        public static byte[] GenerarDocumentoFlexible(string plantillaPath, Dictionary<string, string> metadatos, 
            ContenidoDocumentoDto contenido,
            List<ControlCambioDto>? controlCambios = null,
            List<FirmaAprobadorDto>? aprobadores = null)
        {
            if (!File.Exists(plantillaPath))
                throw new FileNotFoundException("Plantilla no encontrada: " + plantillaPath);

            var docxConMetadatos = ReemplazarPlaceholdersDocxSimple(plantillaPath, metadatos);
            var docxCompleto = InsertarContenidoEnDocx(docxConMetadatos, contenido, controlCambios, aprobadores);

            var tempDir = Path.Combine(Path.GetTempPath(), $"FileNova_{Guid.NewGuid()}");
            Directory.CreateDirectory(tempDir);

            try
            {
                var docxTempPath = Path.Combine(tempDir, "documento.docx");
                File.WriteAllBytes(docxTempPath, docxCompleto);
                return ConvertirDocxAPdf(docxTempPath, tempDir);
            }
            finally
            {
                try { if (Directory.Exists(tempDir)) Directory.Delete(tempDir, true); } catch { }
            }
        }

        // REEMPLAZO DE PLACEHOLDERS (ZIP)
        public static byte[] ReemplazarPlaceholdersDocxSimple(
            string plantillaPath,
            Dictionary<string, string> metadatos)
        {
            var tempDir = Path.Combine(Path.GetTempPath(), $"DocxReplace_{Guid.NewGuid()}");
            Directory.CreateDirectory(tempDir);

            try
            {
                var tempDocx = Path.Combine(tempDir, "temp.docx");
                File.Copy(plantillaPath, tempDocx, true);

                var extractDir = Path.Combine(tempDir, "extracted");
                System.IO.Compression.ZipFile.ExtractToDirectory(tempDocx, extractDir);

                var placeholdersOpenXml = new List<string>
                {
                    "{CONTENIDO}",
                    "{ControlCambios}",
                    "{TablaContenido}"
                };

                var documentXmlPath = Path.Combine(extractDir, "word", "document.xml");
                if (File.Exists(documentXmlPath))
                {
                    var xml = File.ReadAllText(documentXmlPath, Encoding.UTF8);
                    foreach (var item in metadatos)
                    {
                        if (!placeholdersOpenXml.Contains(item.Key))
                        {
                            xml = xml.Replace(item.Key, item.Value ?? "");
                        }
                    }
                    File.WriteAllText(documentXmlPath, xml, Encoding.UTF8);
                }

                var wordDir = Path.Combine(extractDir, "word");
                if (Directory.Exists(wordDir))
                {
                    foreach (var headerFile in Directory.GetFiles(wordDir, "header*.xml"))
                    {
                        var xml = File.ReadAllText(headerFile, Encoding.UTF8);
                        foreach (var item in metadatos)
                        {
                            if (!placeholdersOpenXml.Contains(item.Key))
                                xml = xml.Replace(item.Key, item.Value ?? "");
                        }
                        File.WriteAllText(headerFile, xml, Encoding.UTF8);
                    }
                    foreach (var footerFile in Directory.GetFiles(wordDir, "footer*.xml"))
                    {
                        var xml = File.ReadAllText(footerFile, Encoding.UTF8);
                        foreach (var item in metadatos)
                        {
                            if (!placeholdersOpenXml.Contains(item.Key))
                                xml = xml.Replace(item.Key, item.Value ?? "");
                        }
                        File.WriteAllText(footerFile, xml, Encoding.UTF8);
                    }
                }

                var outputDocx = Path.Combine(tempDir, "output.docx");
                if (File.Exists(outputDocx)) File.Delete(outputDocx);
                System.IO.Compression.ZipFile.CreateFromDirectory(extractDir, outputDocx);

                return File.ReadAllBytes(outputDocx);
            }
            finally
            {
                try { Directory.Delete(tempDir, true); } catch { }
            }
        }

        // INSERCIÓN DE CONTENIDO EN DOCX
        private static byte[] InsertarContenidoEnDocx(byte[] docxBytes, ContenidoDocumentoDto contenido,
            List<ControlCambioDto>? controlCambios,
            List<FirmaAprobadorDto>? aprobadores)
        {
            using var mem = new MemoryStream();
            mem.Write(docxBytes, 0, docxBytes.Length);
            mem.Position = 0;

            using (var wordDoc = WordprocessingDocument.Open(mem, true))
            {
                var body = wordDoc.MainDocumentPart?.Document?.Body;
                if (body == null)
                    throw new Exception("El documento Word no tiene estructura válida");

                // GUARDAR REFERENCIA para insertar imágenes
                _currentMainDocumentPart = wordDoc.MainDocumentPart;

                InsertarTablaContenidoManual(body, contenido);
                InsertarContenido(body, contenido);

                if (controlCambios != null && controlCambios.Any())
                    InsertarTablaControlCambios(body, controlCambios);
            }

            mem.Position = 0;
            return mem.ToArray();
        }

        private static void InsertarContenido(Body body, ContenidoDocumentoDto contenido)
        {
            var placeholder = body.Descendants<Text>()
                .FirstOrDefault(t => t.Text.Contains("{CONTENIDO}"));

            if (placeholder == null) return;

            var run = (Run)placeholder.Parent;
            var paragraph = (Paragraph)run.Parent;

            var bloques = contenido.Bloques.OrderBy(b => b.Orden ?? 0).ToList();
            var ultimo = (OpenXmlElement)paragraph;

            int titulo1Counter = 0, titulo2Counter = 0, titulo3Counter = 0;

            foreach (var bloque in bloques)
            {
                var elementos = CrearElementosDesdeBloque(bloque, ref titulo1Counter, ref titulo2Counter, ref titulo3Counter);
                foreach (var elem in elementos)
                    ultimo = body.InsertAfter(elem, ultimo);
            }

            paragraph.Remove();
        }

        private static List<OpenXmlElement> CrearElementosDesdeBloque(BloqueContenidoDto bloque, ref int titulo1Counter, ref int titulo2Counter,
            ref int titulo3Counter)
        {
            var elementos = new List<OpenXmlElement>();

            switch (bloque.Tipo?.ToLower())
            {
                case "titulo":
                    titulo1Counter++; titulo2Counter = 0; titulo3Counter = 0;
                    elementos.Add(CrearTituloNivel1(bloque.Contenido, titulo1Counter));
                    break;

                case "subtitulo":
                    titulo2Counter++; titulo3Counter = 0;
                    elementos.Add(CrearTituloNivel2(bloque.Contenido, titulo1Counter, titulo2Counter));
                    break;

                case "subtitulo3":
                    titulo3Counter++;
                    elementos.Add(CrearTituloNivel3(bloque.Contenido, titulo1Counter, titulo2Counter, titulo3Counter));
                    break;

                case "texto":
                    elementos.Add(CrearParrafoTexto(bloque.Contenido));
                    break;

                case "lista":
                case "vineta":
                    elementos.AddRange(CrearParrafosLista(bloque.Contenido));
                    break;

                case "nota":
                    elementos.Add(CrearParrafoNota(bloque.Contenido));
                    break;

                case "salto":
                    elementos.Add(new Paragraph(new Run(new Break { Type = BreakValues.Page })));
                    break;

                case "imagen":
                    var pImagen = CrearParrafoImagen(bloque);
                    if (pImagen != null) elementos.Add(pImagen);
                    break;

                case "tabla":
                    var tabla = CrearTablaDesdeBloque(bloque);
                    if (tabla != null) elementos.Add(tabla);
                    elementos.Add(new Paragraph(new ParagraphProperties(
                        new SpacingBetweenLines { After = "120" })));
                    break;

                default:
                    elementos.Add(CrearParrafoTexto(bloque.Contenido));
                    break;
            }

            return elementos;
        }

        // TÍTULOS
        private static Paragraph CrearTituloNivel1(string? texto, int numero)
        {
            var p = new Paragraph();
            var pPr = new ParagraphProperties(
                new ParagraphStyleId { Val = "Heading1" },
                new SpacingBetweenLines { After = "240", Line = "240", LineRule = LineSpacingRuleValues.Auto },
                new Justification { Val = JustificationValues.Left },
                new KeepNext(),
                new KeepLines()
            );
            p.Append(pPr);

            var run = new Run();
            var rPr = new RunProperties(
                new RunFonts { Ascii = FuenteNormal, HighAnsi = FuenteNormal, ComplexScript = FuenteNormal },
                new FontSize { Val = "28" },
                new Bold(),
                new Color { Val = ColorTitulo1 },
                new FontSizeComplexScript { Val = "28" }
            );
            run.PrependChild(rPr);
            run.Append(new Text($"{numero}.  {(texto ?? "").ToUpper()}")
            { Space = SpaceProcessingModeValues.Preserve });
            p.Append(run);

            return p;
        }

        private static Paragraph CrearTituloNivel2(string? texto, int num1, int num2)
        {
            var p = new Paragraph();
            var pPr = new ParagraphProperties(
                new ParagraphStyleId { Val = "Heading2" },
                new SpacingBetweenLines { After = "240", Line = "240", LineRule = LineSpacingRuleValues.Auto },
                new Justification { Val = JustificationValues.Left },
                new KeepNext()
            );
            p.Append(pPr);

            var run = new Run();
            var rPr = new RunProperties(
                new RunFonts { Ascii = FuenteNormal, HighAnsi = FuenteNormal, ComplexScript = FuenteNormal },
                new FontSize { Val = "24" },
                new Bold(),
                new Color { Val = ColorTitulo2 }
            );
            run.PrependChild(rPr);
            run.Append(new Text($"{num1}.{num2}  {texto ?? ""}")
            { Space = SpaceProcessingModeValues.Preserve });
            p.Append(run);

            return p;
        }

        private static Paragraph CrearTituloNivel3(string? texto, int num1, int num2, int num3)
        {
            var p = new Paragraph();
            var pPr = new ParagraphProperties(
                new ParagraphStyleId { Val = "Heading3" },
                new SpacingBetweenLines { After = "240", Line = "240", LineRule = LineSpacingRuleValues.Auto },
                new Justification { Val = JustificationValues.Left },
                new KeepNext()
            );
            p.Append(pPr);

            var run = new Run();
            var rPr = new RunProperties(
                new RunFonts { Ascii = FuenteNormal, HighAnsi = FuenteNormal, ComplexScript = FuenteNormal },
                new FontSize { Val = "24" },
                new Color { Val = ColorTexto }
            );
            run.PrependChild(rPr);
            run.Append(new Text($"{num1}.{num2}.{num3}  {texto ?? ""}")
            { Space = SpaceProcessingModeValues.Preserve });
            p.Append(run);

            return p;
        }

        // PÁRRAFOS
        private static Paragraph CrearParrafoTexto(string? texto)
        {
            var p = new Paragraph();
            var pPr = new ParagraphProperties(
                new SpacingBetweenLines { After = "120", Line = "240", LineRule = LineSpacingRuleValues.Auto },
                new Justification { Val = JustificationValues.Both }
            );
            p.Append(pPr);

            if (!string.IsNullOrEmpty(texto))
            {
                var run = new Run();
                var rPr = new RunProperties(
                    new RunFonts { Ascii = FuenteNormal, HighAnsi = FuenteNormal, ComplexScript = FuenteNormal },
                    new FontSize { Val = TamanoContenido },
                    new Color { Val = ColorTexto }
                );
                run.PrependChild(rPr);

                var lineas = texto.Split('\n');
                for (int i = 0; i < lineas.Length; i++)
                {
                    if (i > 0) run.Append(new Break());
                    run.Append(new Text(lineas[ i ]) { Space = SpaceProcessingModeValues.Preserve });
                }
                p.Append(run);
            }
            return p;
        }

        private static List<Paragraph> CrearParrafosLista(string? texto)
        {
            var parrafos = new List<Paragraph>();
            if (string.IsNullOrEmpty(texto)) return parrafos;

            foreach (var item in texto.Split('\n').Where(l => !string.IsNullOrWhiteSpace(l)))
            {
                var p = new Paragraph();
                var pPr = new ParagraphProperties(
                    new SpacingBetweenLines { After = "60", Line = "240", LineRule = LineSpacingRuleValues.Auto },
                    new Indentation { Left = "720" },
                    new Justification { Val = JustificationValues.Left }
                );
                p.Append(pPr);

                var run = new Run();
                var rPr = new RunProperties(
                    new RunFonts { Ascii = FuenteNormal, HighAnsi = FuenteNormal, ComplexScript = FuenteNormal },
                    new FontSize { Val = TamanoContenido },
                    new Color { Val = ColorTexto }
                );
                run.PrependChild(rPr);
                run.Append(new Text("• " + item.Trim()) { Space = SpaceProcessingModeValues.Preserve });
                p.Append(run);
                parrafos.Add(p);
            }
            return parrafos;
        }

        private static Paragraph CrearParrafoNota(string? texto)
        {
            var p = new Paragraph();
            var pPr = new ParagraphProperties(
                new ParagraphBorders(
                    new LeftBorder { Val = BorderValues.Single, Size = 24, Color = "FFC107" }),
                new Shading { Fill = "FFF3CD" },
                new SpacingBetweenLines { After = "200" }
            );
            p.Append(pPr);

            var runTitulo = new Run();
            runTitulo.PrependChild(new RunProperties(
                new RunFonts { Ascii = FuenteNormal, HighAnsi = FuenteNormal },
                new Bold(),
                new Color { Val = "856404" },
                new FontSize { Val = TamanoContenido }
            ));
            runTitulo.Append(new Text("Nota: ") { Space = SpaceProcessingModeValues.Preserve });
            p.Append(runTitulo);

            var runTexto = new Run();
            runTexto.PrependChild(new RunProperties(
                new RunFonts { Ascii = FuenteNormal, HighAnsi = FuenteNormal },
                new FontSize { Val = TamanoContenido }
            ));
            runTexto.Append(new Text(texto ?? "") { Space = SpaceProcessingModeValues.Preserve });
            p.Append(runTexto);

            return p;
        }

        // IMAGEN
        private static Paragraph? CrearParrafoImagen(BloqueContenidoDto bloque)
        {
            var p = new Paragraph();
            var pPr = new ParagraphProperties(
                new Justification { Val = JustificationValues.Center },
                new SpacingBetweenLines { After = "200" }
            );
            p.Append(pPr);

            // Obtener caption de forma segura
            var caption = "";
            if (bloque.Metadatos != null && bloque.Metadatos.ContainsKey("pie"))
            {
                caption = bloque.Metadatos[ "pie" ]?.ToString() ?? "";
            }

            var urlImagen = bloque.UrlImagen ?? "";

            Console.WriteLine($"🖼️ CrearParrafoImagen - UrlImagen tipo: {(urlImagen.StartsWith("data:image") ? "BASE64" : urlImagen.StartsWith("/archivos/") ? "RUTA LOCAL" : "OTRO")}");
            Console.WriteLine($"   UrlImagen longitud: {urlImagen.Length}");
            Console.WriteLine($"   Caption: '{caption}'");

            bool imagenInsertada = false;

            // ✅ CASO 1: La imagen ya es base64 (viene de PrepararImagenesParaPdf)
            if (urlImagen.StartsWith("data:image"))
            {
                try
                {
                    // Extraer el base64 y convertirlo a bytes
                    var base64Data = urlImagen.Split(',')[ 1 ];
                    var imageBytes = Convert.FromBase64String(base64Data);

                    // Determinar tipo de imagen desde el base64
                    string extension = "png";
                    if (urlImagen.Contains("image/jpeg") || urlImagen.Contains("image/jpg"))
                        extension = "jpg";
                    else if (urlImagen.Contains("image/png"))
                        extension = "png";
                    else if (urlImagen.Contains("image/gif"))
                        extension = "gif";
                    else if (urlImagen.Contains("image/webp"))
                        extension = "webp";

                    // Guardar temporalmente para insertar
                    var tempFile = Path.Combine(Path.GetTempPath(), $"img_{Guid.NewGuid()}.{extension}");
                    File.WriteAllBytes(tempFile, imageBytes);

                    try
                    {
                        // ✅ Usar tamaño predeterminado
                        imagenInsertada = InsertarImagenEnParrafo(p, tempFile, true); // true = usar tamaño predeterminado
                        Console.WriteLine($"   Insertada desde base64 con tamaño predeterminado: {imagenInsertada}");
                    }
                    finally
                    {
                        // Limpiar archivo temporal
                        try { File.Delete(tempFile); } catch { }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"   Error procesando base64: {ex.Message}");
                }
            }
            // ✅ CASO 2: Es ruta local (para desarrollo o depuración)
            else if (!string.IsNullOrEmpty(urlImagen) && !urlImagen.StartsWith("__IMAGEN_") && File.Exists(urlImagen))
            {
                try
                {
                    imagenInsertada = InsertarImagenEnParrafo(p, urlImagen, true);
                    Console.WriteLine($"   Insertada desde ruta con tamaño predeterminado: {imagenInsertada}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"   Error insertando desde ruta: {ex.Message}");
                }
            }
            // ✅ CASO 3: Es ruta virtual del servidor (para desarrollo)
            else if (!string.IsNullOrEmpty(urlImagen) && urlImagen.StartsWith("/archivos/"))
            {
                try
                {
                    var rutaAbsoluta = Path.Combine(
                        Directory.GetCurrentDirectory(), "wwwroot",
                        urlImagen.TrimStart('/').TrimStart('/').Replace("/", "\\"));

                    if (File.Exists(rutaAbsoluta))
                    {
                        imagenInsertada = InsertarImagenEnParrafo(p, rutaAbsoluta, true);
                        Console.WriteLine($"   Insertada desde ruta virtual con tamaño predeterminado: {imagenInsertada}");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"   Error insertando desde ruta virtual: {ex.Message}");
                }
            }

            // Placeholder si no se insertó
            if (!imagenInsertada)
            {
                Console.WriteLine($"   ⚠️ No se pudo insertar imagen, usando placeholder");
                var run = new Run();
                run.PrependChild(new RunProperties(
                    new RunFonts { Ascii = FuenteNormal, HighAnsi = FuenteNormal },
                    new FontSize { Val = TamanoFiguras },
                    new Italic(),
                    new Color { Val = "999999" }
                ));
                run.Append(new Text(
                    string.IsNullOrEmpty(caption) ? "[Imagen no disponible]" : $"[Imagen: {caption}]")
                { Space = SpaceProcessingModeValues.Preserve });
                p.Append(run);
            }

            // Pie de imagen
            if (!string.IsNullOrEmpty(caption))
            {
                var runCaption = new Run();
                runCaption.PrependChild(new RunProperties(
                    new RunFonts { Ascii = FuenteNormal, HighAnsi = FuenteNormal },
                    new FontSize { Val = TamanoFiguras },
                    new Italic(),
                    new Color { Val = "666666" }
                ));
                runCaption.Append(new Break());
                runCaption.Append(new Text($"Figura: {caption}")
                { Space = SpaceProcessingModeValues.Preserve });
                p.Append(runCaption);
            }

            return p;
        }



        private static bool InsertarImagenEnParrafo(Paragraph paragraph, string rutaImagen, bool usarTamanoPredeterminado = false)
        {
            try
            {
                if (_currentMainDocumentPart == null)
                {
                    Console.WriteLine("❌ _currentMainDocumentPart es null");
                    return false;
                }

                var extension = Path.GetExtension(rutaImagen).ToLower();

                ImagePart imagePart;

                switch (extension)
                {
                    case ".png":
                        imagePart = _currentMainDocumentPart.AddImagePart(ImagePartType.Png);
                        break;
                    case ".jpg":
                    case ".jpeg":
                        imagePart = _currentMainDocumentPart.AddImagePart(ImagePartType.Jpeg);
                        break;
                    default:
                        imagePart = _currentMainDocumentPart.AddImagePart(ImagePartType.Png);
                        break;
                }

                // Alimentar la imagen al part
                using (var stream = new FileStream(rutaImagen, FileMode.Open, FileAccess.Read))
                {
                    imagePart.FeedData(stream);
                }

                // Obtener dimensiones (con tamaño predeterminado si se solicita)
                long cx, cy;

                if (usarTamanoPredeterminado)
                {
                    // Tamaño predeterminado: ~15cm de ancho (5,669,291 EMUs)
                    const long defaultWidthEmu = 5669291;  // 15cm
                    const long defaultHeightEmu = 4251968; // 11.25cm (proporción 4:3)

                    cx = defaultWidthEmu;
                    cy = defaultHeightEmu;
                    Console.WriteLine($"   Usando tamaño predeterminado: {cx}x{cy} EMU");
                }
                else
                {
                    // Calcular dimensiones originales
                    (cx, cy) = ObtenerDimensionesImagen(rutaImagen);
                }

                // Obtener el ID de la relación
                var imageId = _currentMainDocumentPart.GetIdOfPart(imagePart);

                // ID único para DocProperties
                var docPrId = (uint)new Random().Next(1000, 99999);
                var pictureId = (uint)new Random().Next(1, 999);

                // Construir el elemento Drawing
                var drawing = new Drawing(
                    new DW.Inline(
                        new DW.Extent { Cx = cx, Cy = cy },
                        new DW.EffectExtent
                        {
                            LeftEdge = 0L,
                            TopEdge = 0L,
                            RightEdge = 0L,
                            BottomEdge = 0L
                        },
                        new DW.DocProperties
                        {
                            Id = docPrId,
                            Name = Path.GetFileName(rutaImagen)
                        },
                        new DW.NonVisualGraphicFrameDrawingProperties(
                            new A.GraphicFrameLocks { NoChangeAspect = true }),
                        new A.Graphic(
                            new A.GraphicData(
                                new PIC.Picture(
                                    new PIC.NonVisualPictureProperties(
                                        new PIC.NonVisualDrawingProperties
                                        {
                                            Id = pictureId,
                                            Name = Path.GetFileName(rutaImagen)
                                        },
                                        new PIC.NonVisualPictureDrawingProperties()),
                                    new PIC.BlipFill(
                                        new A.Blip { Embed = imageId },
                                        new A.Stretch(new A.FillRectangle())),
                                    new PIC.ShapeProperties(
                                        new A.Transform2D(
                                            new A.Offset { X = 0L, Y = 0L },
                                            new A.Extents { Cx = cx, Cy = cy }),
                                        new A.PresetGeometry(new A.AdjustValueList())
                                        { Preset = A.ShapeTypeValues.Rectangle })
                                )
                            )
                            { Uri = "http://schemas.openxmlformats.org/drawingml/2006/picture" }
                        )
                    )
                    {
                        DistanceFromTop = 0,
                        DistanceFromBottom = 0,
                        DistanceFromLeft = 0,
                        DistanceFromRight = 0
                    }
                );

                var run = new Run(drawing);
                paragraph.Append(run);

                Console.WriteLine($"✅ Imagen insertada correctamente: {rutaImagen} (Tamaño: {cx}x{cy} EMU)");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error en InsertarImagenEnParrafo: {ex.Message}");
                return false;
            }
        }

        private static (long cx, long cy) ObtenerDimensionesImagen(string rutaImagen)
        {
            // Tamaño máximo: ~16cm de ancho en EMUs
            const long maxWidthEmu = 6000000;
            const long defaultHeightEmu = 4500000; // ~12cm (proporción 4:3)

            try
            {
                // Leer los primeros bytes para detectar dimensiones de PNG/JPEG
                var bytes = File.ReadAllBytes(rutaImagen);

                if (bytes.Length < 24) return (maxWidthEmu, defaultHeightEmu);

                int width = 0, height = 0;

                // Detectar tipo por firma
                if (bytes[ 0 ] == 0x89 && bytes[ 1 ] == 0x50) // PNG
                {
                    // Dimensiones en bytes 16-23 (big-endian)
                    width = (bytes[ 16 ] << 24) | (bytes[ 17 ] << 16) | (bytes[ 18 ] << 8) | bytes[ 19 ];
                    height = (bytes[ 20 ] << 24) | (bytes[ 21 ] << 16) | (bytes[ 22 ] << 8) | bytes[ 23 ];
                }
                else if (bytes[ 0 ] == 0xFF && bytes[ 1 ] == 0xD8) // JPEG
                {
                    // Buscar segmento SOF0 (0xC0) que contiene dimensiones
                    int pos = 2;
                    while (pos < bytes.Length - 9)
                    {
                        if (bytes[ pos ] == 0xFF && (bytes[ pos + 1 ] == 0xC0 || bytes[ pos + 1 ] == 0xC2))
                        {
                            height = (bytes[ pos + 5 ] << 8) | bytes[ pos + 6 ];
                            width = (bytes[ pos + 7 ] << 8) | bytes[ pos + 8 ];
                            break;
                        }
                        pos += 2 + ((bytes[ pos + 2 ] << 8) | bytes[ pos + 3 ]);
                    }
                }
                else if (bytes[ 0 ] == 0x47 && bytes[ 1 ] == 0x49) // GIF
                {
                    width = bytes[ 6 ] | (bytes[ 7 ] << 8);
                    height = bytes[ 8 ] | (bytes[ 9 ] << 8);
                }
                else if (bytes[ 0 ] == 0x42 && bytes[ 1 ] == 0x4D) // BMP
                {
                    width = bytes[ 18 ] | (bytes[ 19 ] << 8) | (bytes[ 20 ] << 16) | (bytes[ 21 ] << 24);
                    height = bytes[ 22 ] | (bytes[ 23 ] << 8) | (bytes[ 24 ] << 16) | (bytes[ 25 ] << 24);
                }

                if (width > 0 && height > 0)
                {
                    double ratio = (double)height / width;
                    long cx = Math.Min(maxWidthEmu, (long)(width * 9525));
                    long cy = (long)(cx * ratio);
                    Console.WriteLine($"   Dimensiones: {width}x{height}px → {cx}x{cy} EMU");
                    return (cx, cy);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"   ⚠️ No se pudieron leer dimensiones: {ex.Message}");
            }

            // Valor por defecto si no se detectan
            return (maxWidthEmu, defaultHeightEmu);
        }

        // TABLA DESDE BLOQUE
        private static Table? CrearTablaDesdeBloque(BloqueContenidoDto bloque)
        {
            if (bloque.Metadatos == null) return null;

            try
            {
                // Funciones de deserialización
                List<string> DeserializarColumnas(object? valor)
                {
                    if (valor == null) return new List<string> { "Columna 1" };

                    if (valor is System.Text.Json.JsonElement jsonElement)
                    {
                        var result = new List<string>();
                        foreach (var item in jsonElement.EnumerateArray())
                        {
                            result.Add(item.GetString() ?? "");
                        }
                        return result.Count > 0 ? result : new List<string> { "Columna 1" };
                    }

                    var str = valor.ToString();
                    if (string.IsNullOrEmpty(str)) return new List<string> { "Columna 1" };

                    try
                    {
                        return System.Text.Json.JsonSerializer.Deserialize<List<string>>(str)
                            ?? new List<string> { "Columna 1" };
                    }
                    catch
                    {
                        return new List<string> { "Columna 1" };
                    }
                }

                List<List<string>> DeserializarFilas(object? valor)
                {
                    if (valor == null) return new List<List<string>>();

                    if (valor is System.Text.Json.JsonElement jsonElement)
                    {
                        var result = new List<List<string>>();
                        foreach (var fila in jsonElement.EnumerateArray())
                        {
                            var filaList = new List<string>();
                            foreach (var celda in fila.EnumerateArray())
                            {
                                filaList.Add(celda.GetString() ?? "");
                            }
                            result.Add(filaList);
                        }
                        return result.Count > 0 ? result : new List<List<string>>();
                    }

                    var str = valor.ToString();
                    if (string.IsNullOrEmpty(str)) return new List<List<string>>();

                    try
                    {
                        return System.Text.Json.JsonSerializer.Deserialize<List<List<string>>>(str)
                            ?? new List<List<string>>();
                    }
                    catch
                    {
                        return new List<List<string>>();
                    }
                }

                // Obtener columnas
                List<string> columnas;
                if (bloque.Metadatos.ContainsKey("columnas"))
                {
                    columnas = DeserializarColumnas(bloque.Metadatos[ "columnas" ]);
                }
                else
                {
                    columnas = new List<string> { "Columna 1" };
                }

                // Obtener filas
                List<List<string>> filas;
                if (bloque.Metadatos.ContainsKey("filas"))
                {
                    filas = DeserializarFilas(bloque.Metadatos[ "filas" ]);
                }
                else
                {
                    filas = new List<List<string>>();
                }

                // Si no hay filas, crear una fila vacía
                if (filas.Count == 0)
                {
                    filas.Add(new List<string>());
                }

                // Asegurar que todas las filas tengan el mismo número de columnas
                for (int i = 0; i < filas.Count; i++)
                {
                    while (filas[ i ].Count < columnas.Count)
                    {
                        filas[ i ].Add("");
                    }
                }

                // Verificar transpuesta
                bool transpuesta = false;
                if (bloque.Metadatos.ContainsKey("transpuesta"))
                {
                    var valor = bloque.Metadatos[ "transpuesta" ];
                    if (valor is System.Text.Json.JsonElement jsonElement)
                    {
                        if (jsonElement.ValueKind == System.Text.Json.JsonValueKind.True)
                            transpuesta = true;
                        else if (jsonElement.ValueKind == System.Text.Json.JsonValueKind.String)
                            transpuesta = jsonElement.GetString()?.ToLower() == "true";
                    }
                    else
                    {
                        transpuesta = valor?.ToString()?.ToLower() == "true";
                    }
                }

                Console.WriteLine($"📊 Columnas: {columnas.Count}, Filas: {filas.Count}, Transpuesta: {transpuesta}");

                // Si está transpuesta, reorganizar
                if (transpuesta)
                {
                    var titulosFilas = columnas;
                    var datos = filas;

                    var nuevasColumnas = new List<string> { "" };

                    int numColumnasDatos = 0;
                    foreach (var f in datos)
                    {
                        if (f.Count > numColumnasDatos)
                            numColumnasDatos = f.Count;
                    }

                    for (int i = 0; i < numColumnasDatos; i++)
                    {
                        nuevasColumnas.Add("");
                    }

                    var nuevasFilas = new List<List<string>>();
                    for (int i = 0; i < titulosFilas.Count; i++)
                    {
                        var nuevaFila = new List<string> { titulosFilas[ i ] };

                        if (i < datos.Count)
                        {
                            for (int j = 0; j < numColumnasDatos; j++)
                            {
                                nuevaFila.Add(j < datos[ i ].Count ? datos[ i ][ j ] : "");
                            }
                        }
                        else
                        {
                            for (int j = 0; j < numColumnasDatos; j++)
                            {
                                nuevaFila.Add("");
                            }
                        }

                        nuevasFilas.Add(nuevaFila);
                    }

                    columnas = nuevasColumnas;
                    filas = nuevasFilas;
                }

                // ✅ CREAR TABLA (UNA SOLA VEZ)
                var tabla = new Table();
                var tblPr = new TableProperties(
                    new TableWidth { Width = "5000", Type = TableWidthUnitValues.Pct },
                    new TableBorders(
                        new TopBorder { Val = BorderValues.Single, Size = 8, Color = "999999" },
                        new BottomBorder { Val = BorderValues.Single, Size = 8, Color = "999999" },
                        new LeftBorder { Val = BorderValues.Single, Size = 8, Color = "999999" },
                        new RightBorder { Val = BorderValues.Single, Size = 8, Color = "999999" },
                        new InsideHorizontalBorder { Val = BorderValues.Single, Size = 6, Color = "BBBBBB" },
                        new InsideVerticalBorder { Val = BorderValues.Single, Size = 6, Color = "BBBBBB" }
                    ),
                    new TableLook { Val = "04A0" }
                );
                tabla.AppendChild(tblPr);

                // ✅ ENCABEZADO - Solo si no es transpuesta o hay columnas con texto
                bool mostrarEncabezado = !transpuesta || columnas.Any(c => !string.IsNullOrEmpty(c));

                if (mostrarEncabezado)
                {
                    var headerRow = new TableRow();
                    headerRow.AppendChild(new TableRowProperties(
                        new TableRowHeight { Val = 400, HeightType = HeightRuleValues.AtLeast }));

                    foreach (var col in columnas)
                    {
                        var cell = new TableCell();
                        cell.AppendChild(new TableCellProperties(
                            new Shading { Fill = ColorTablaEncabezado, Val = ShadingPatternValues.Clear },
                            new TableCellVerticalAlignment { Val = TableVerticalAlignmentValues.Center }));
                        var p = new Paragraph();
                        p.AppendChild(new ParagraphProperties(
                            new Justification { Val = JustificationValues.Center },
                            new SpacingBetweenLines { Before = "40", After = "40" }));
                        var run = new Run();
                        run.PrependChild(new RunProperties(
                            new RunFonts { Ascii = FuenteNormal, HighAnsi = FuenteNormal, ComplexScript = FuenteNormal },
                            new FontSize { Val = TamanoContenido }, new Bold(), new Color { Val = "000000" }));
                        run.Append(new Text(col) { Space = SpaceProcessingModeValues.Preserve });
                        p.Append(run); cell.Append(p); headerRow.Append(cell);
                    }
                    tabla.Append(headerRow);
                }

                // ✅ FILAS DE DATOS
                foreach (var fila in filas)
                {
                    var row = new TableRow();
                    row.AppendChild(new TableRowProperties(
                        new TableRowHeight { Val = 350, HeightType = HeightRuleValues.AtLeast }));

                    for (int i = 0; i < columnas.Count; i++)
                    {
                        var cell = new TableCell();

                        if (transpuesta && i == 0)
                        {
                            cell.AppendChild(new TableCellProperties(
                                new Shading { Fill = ColorTablaEncabezado, Val = ShadingPatternValues.Clear },
                                new TableCellVerticalAlignment { Val = TableVerticalAlignmentValues.Center }));
                        }
                        else
                        {
                            cell.AppendChild(new TableCellProperties(
                                new TableCellVerticalAlignment { Val = TableVerticalAlignmentValues.Center }));
                        }

                        var p = new Paragraph();
                        p.AppendChild(new ParagraphProperties(
                            new SpacingBetweenLines { Before = "20", After = "20" }));

                        var valor = i < fila.Count ? fila[ i ] : "";
                        var run = new Run();
                        var rPr = new RunProperties(
                            new RunFonts { Ascii = FuenteNormal, HighAnsi = FuenteNormal, ComplexScript = FuenteNormal },
                            new FontSize { Val = TamanoContenido },
                            new Color { Val = ColorTexto });

                        if (transpuesta && i == 0)
                        {
                            rPr.Append(new Bold());
                        }

                        run.PrependChild(rPr);
                        run.Append(new Text(valor) { Space = SpaceProcessingModeValues.Preserve });
                        p.Append(run);
                        cell.Append(p);
                        row.Append(cell);
                    }
                    tabla.Append(row);
                }

                return tabla;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error creando tabla: {ex.Message}");
                return null;
            }
        }

        // TABLAS DEL SISTEMA
        private static void InsertarTablaControlCambios(Body body, List<ControlCambioDto> cambios)
        {
            var placeholder = body.Descendants<Text>().FirstOrDefault(t => t.Text.Contains("{ControlCambios}"));
            if (placeholder == null) return;
            var paragraph = (Paragraph)placeholder.Parent!.Parent!;
            paragraph.RemoveAllChildren();

            var tabla = new Table();
            tabla.AppendChild(new TableProperties(
                new TableWidth { Width = "5000", Type = TableWidthUnitValues.Pct },
                new TableBorders(
                    new TopBorder { Val = BorderValues.Single, Size = 8, Color = "999999" },
                    new BottomBorder { Val = BorderValues.Single, Size = 8, Color = "999999" },
                    new LeftBorder { Val = BorderValues.Single, Size = 8, Color = "999999" },
                    new RightBorder { Val = BorderValues.Single, Size = 8, Color = "999999" },
                    new InsideHorizontalBorder { Val = BorderValues.Single, Size = 6, Color = "BBBBBB" },
                    new InsideVerticalBorder { Val = BorderValues.Single, Size = 6, Color = "BBBBBB" })));
            tabla.Append(CrearFilaEncabezado("VERSIÓN", "FECHA", "CREADO POR", "DESCRIPCIÓN DEL CAMBIO"));
            foreach (var c in cambios)
                tabla.Append(CrearFilaNormal(c.Version ?? "", c.Fecha ?? "", c.Usuario ?? "", c.Descripcion ?? ""));
            paragraph.InsertAfterSelf(tabla);
        }

        private static TableRow CrearFilaEncabezado(params string[] valores)
        {
            var fila = new TableRow();
            foreach (var val in valores)
            {
                var cell = new TableCell();
                cell.AppendChild(new TableCellProperties(
                    new Shading { Fill = ColorTablaEncabezado, Val = ShadingPatternValues.Clear },
                    new TableCellVerticalAlignment { Val = TableVerticalAlignmentValues.Center }));
                var p = new Paragraph();
                p.AppendChild(new ParagraphProperties(
                    new Justification { Val = JustificationValues.Center },
                    new SpacingBetweenLines { Before = "40", After = "40" }));
                var run = new Run();
                run.PrependChild(new RunProperties(
                    new RunFonts { Ascii = FuenteNormal, HighAnsi = FuenteNormal, ComplexScript = FuenteNormal },
                    new FontSize { Val = TamanoContenido }, new Bold()));
                run.Append(new Text(val) { Space = SpaceProcessingModeValues.Preserve });
                p.Append(run); cell.Append(p); fila.Append(cell);
            }
            return fila;
        }

        private static TableRow CrearFilaNormal(params string[] valores)
        {
            var fila = new TableRow();
            foreach (var val in valores)
            {
                var cell = new TableCell();
                cell.AppendChild(new TableCellProperties(
                    new TableCellVerticalAlignment { Val = TableVerticalAlignmentValues.Center }));
                var p = new Paragraph();
                p.AppendChild(new ParagraphProperties(new SpacingBetweenLines { Before = "20", After = "20" }));
                var run = new Run();
                run.PrependChild(new RunProperties(
                    new RunFonts { Ascii = FuenteNormal, HighAnsi = FuenteNormal, ComplexScript = FuenteNormal },
                    new FontSize { Val = TamanoContenido }));
                run.Append(new Text(val) { Space = SpaceProcessingModeValues.Preserve });
                p.Append(run); cell.Append(p); fila.Append(cell);
            }
            return fila;
        }

        // TABLA DE CONTENIDO MANUAL
        private static void InsertarTablaContenidoManual(Body body, ContenidoDocumentoDto contenido)
        {
            var placeholder = body.Descendants<Text>().FirstOrDefault(t => t.Text.Contains("{TablaContenido}"));
            if (placeholder == null) return;

            var runPlaceholder = (Run)placeholder.Parent;
            var paragraphPlaceholder = (Paragraph)runPlaceholder.Parent;

            var tituloToc = new Paragraph();
            tituloToc.Append(new ParagraphProperties(
                new ParagraphStyleId { Val = "Heading1" },
                new SpacingBetweenLines { After = "240", Line = "240", LineRule = LineSpacingRuleValues.Auto },
                new Justification { Val = JustificationValues.Left }, new KeepNext(), new KeepLines(), new PageBreakBefore()));
            var runTitulo = new Run();
            runTitulo.PrependChild(new RunProperties(
                new RunFonts { Ascii = FuenteNormal, HighAnsi = FuenteNormal, ComplexScript = FuenteNormal },
                new FontSize { Val = "28" }, new Bold(), new Color { Val = ColorTitulo1 }));
            runTitulo.Append(new Text("TABLA DE CONTENIDO") { Space = SpaceProcessingModeValues.Preserve });
            tituloToc.Append(runTitulo);
            paragraphPlaceholder.InsertAfterSelf(tituloToc);
            var ultimo = (OpenXmlElement)tituloToc;

            var bloques = contenido.Bloques.OrderBy(b => b.Orden ?? 0).ToList();
            int t1 = 0, t2 = 0, t3 = 0;

            foreach (var bloque in bloques)
            {
                string? textoEntrada = null; string fontSize = "24"; bool esNegrita = false; string sangria = "";
                switch (bloque.Tipo?.ToLower())
                {
                    case "titulo": t1++; t2 = 0; t3 = 0; textoEntrada = $"{t1}.  {bloque.Contenido?.ToUpper()}"; esNegrita = true; sangria = "0"; break;
                    case "subtitulo": t2++; t3 = 0; textoEntrada = $"{t1}.{t2}  {bloque.Contenido}"; sangria = "360"; break;
                    case "subtitulo3": t3++; textoEntrada = $"{t1}.{t2}.{t3}  {bloque.Contenido}"; sangria = "720"; break;
                }
                if (textoEntrada != null)
                {
                    var entradaP = new Paragraph();
                    entradaP.Append(new ParagraphProperties(
                        new SpacingBetweenLines { After = "80", Line = "240", LineRule = LineSpacingRuleValues.Auto },
                        new Justification { Val = JustificationValues.Left }, new Indentation { Left = sangria }));
                    var entradaRun = new Run();
                    var entradaRPr = new RunProperties(
                        new RunFonts { Ascii = FuenteNormal, HighAnsi = FuenteNormal, ComplexScript = FuenteNormal },
                        new FontSize { Val = fontSize }, new Color { Val = ColorTexto });
                    if (esNegrita) entradaRPr.Append(new Bold());
                    entradaRun.PrependChild(entradaRPr);
                    entradaRun.Append(new Text(textoEntrada) { Space = SpaceProcessingModeValues.Preserve });
                    entradaP.Append(entradaRun);
                    ultimo = body.InsertAfter(entradaP, ultimo);
                }
            }

            var separador = new Paragraph();
            separador.Append(new ParagraphProperties(
                new ParagraphBorders(new BottomBorder { Val = BorderValues.Single, Size = 4, Color = "999999", Space = 1 }),
                new SpacingBetweenLines { After = "200" }));
            body.InsertAfter(separador, ultimo);
            body.InsertAfter(new Paragraph(new Run(new Break { Type = BreakValues.Page })), separador);
            paragraphPlaceholder.Remove();
        }

        // CONVERSIÓN A PDF
        public static byte[] ConvertirDocxAPdf(string docxPath, string? tempDir = null)
        {
            var dir = tempDir ?? Path.GetDirectoryName(docxPath)!;
            var startInfo = new ProcessStartInfo
            {
                FileName = LibreOfficePath,
                Arguments = $"--headless --convert-to pdf \"{docxPath}\" --outdir \"{dir}\"",
                UseShellExecute = false,
                RedirectStandardError = true,
                CreateNoWindow = true,
                WorkingDirectory = dir
            };
            using var process = Process.Start(startInfo);
            process?.WaitForExit(45000);
            if (process?.ExitCode != 0)
                throw new Exception($"LibreOffice error: {process?.StandardError.ReadToEnd() ?? "Error"}");
            var pdfPath = Path.Combine(dir, Path.GetFileNameWithoutExtension(docxPath) + ".pdf");
            if (!File.Exists(pdfPath)) throw new Exception("No se generó el PDF");
            return File.ReadAllBytes(pdfPath);
        }
    }
}