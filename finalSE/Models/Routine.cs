public class Routine
{
    public int Id { get; set; }
    public string Title { get; set; }
    public string Type { get; set; } // Class / Exam
    public string FilePath { get; set; }
    public DateTime UploadedAt { get; set; }

    // Department wise
    public int? DepartmentId { get; set; }
    public virtual finalSE.Models.DepartmentModel? Department { get; set; }
}