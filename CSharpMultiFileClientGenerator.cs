    using NJsonSchema.CodeGeneration;
    using NJsonSchema.CodeGeneration.CSharp;
    using NSwag;
    using NSwag.CodeGeneration;
    using NSwag.CodeGeneration.CSharp;
    using NSwag.CodeGeneration.CSharp.Models;
    using System.IO;
    using System.Linq;

    public class CSharpMultiFileClientGenerator : CSharpClientGenerator
    {
        private readonly CSharpGeneratorBaseSettings _settings;
        private readonly OpenApiDocument _document;

        public CSharpMultiFileClientGenerator(NSwag.OpenApiDocument document, CSharpClientGeneratorSettings settings) : base(document, settings)
        {
            _document = document;
            _settings = settings;
        }

        public CSharpMultiFileClientGenerator(NSwag.OpenApiDocument document, CSharpClientGeneratorSettings settings, NJsonSchema.CodeGeneration.CSharp.CSharpTypeResolver resolver) : base(document, settings, resolver)
        {
            _document = document;
            _settings = settings;
        }

        public void GenerateFiles(string path)
        {
            GenerateFiles(path, ClientGeneratorOutputType.Full);
        }

        public void GenerateFiles(string path, ClientGeneratorOutputType outputType)
        {
            var clientTypes = GenerateAllClientTypes();

            var dtoTypes = BaseSettings.GenerateDtoTypes ?
                GenerateDtoTypes() :
                Enumerable.Empty<CodeArtifact>();

            clientTypes =
                outputType == ClientGeneratorOutputType.Full ? clientTypes :
                outputType == ClientGeneratorOutputType.Implementation ? clientTypes.Where(t => t.Category != CodeArtifactCategory.Contract) :
                outputType == ClientGeneratorOutputType.Contracts ? clientTypes.Where(t => t.Category == CodeArtifactCategory.Contract) :
                Enumerable.Empty<CodeArtifact>();

            dtoTypes =
                outputType == ClientGeneratorOutputType.Full ||
                outputType == ClientGeneratorOutputType.Contracts ? dtoTypes : Enumerable.Empty<CodeArtifact>();



            foreach (var codeArtifact in clientTypes)
            {
                GenerateClientFile($"{codeArtifact.TypeName}.cs", path, codeArtifact, outputType);
            }

            foreach (var codeArtifact in dtoTypes)
            {
                GenerateDtoFile($"{codeArtifact.TypeName}.cs", path, codeArtifact, outputType);
            }
        }

        protected void GenerateClientFile(string fileName, string path, CodeArtifact clientType, ClientGeneratorOutputType outputType)
        {
            CreatePathIfNotExists(path);
            var model = new CSharpFileTemplateModel(new CodeArtifact[] { clientType }, new CodeArtifact[] { }, outputType, _document, _settings, this, (CSharpTypeResolver)Resolver);
            var result = RenderModel(model);
            GenerateFile(fileName, JoinPath(path, "clients"), result);
        }

        protected void GenerateDtoFile(string fileName, string path, CodeArtifact dtoType, ClientGeneratorOutputType outputType)
        {
            var model = new CSharpFileTemplateModel(new CodeArtifact[] { }, new CodeArtifact[] { dtoType }, outputType, _document, _settings, this, (CSharpTypeResolver)Resolver);
            var result = RenderModel(model);
            GenerateFile(fileName, JoinPath(path, "dtos"), result);
        }

        private string RenderModel(CSharpFileTemplateModel model)
        {
            var template = _settings.CodeGeneratorSettings.TemplateFactory.CreateTemplate("CSharp", "File", model);
            return template.Render();
        }

        private void GenerateFile(string fileName, string path, string contents)
        {
            CreatePathIfNotExists(path);
            var filePath = JoinPath(path, fileName);
            DeleteFileIfExists(filePath);
            var sw = new StreamWriter(filePath);
            sw.Write(contents);
            sw.Close();
            sw.Dispose();
        }

        protected void DeleteFileIfExists(string path)
        {
            if (File.Exists(path))
                File.Delete(path);
        }

        protected void CreatePathIfNotExists(string path)
        {
            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);
        }

        protected string JoinPath(string basePath, string additionalPath)
        {
            return Path.Combine(basePath, additionalPath);
        }
    }
