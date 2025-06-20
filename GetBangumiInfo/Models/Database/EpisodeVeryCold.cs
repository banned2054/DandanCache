using System.ComponentModel.DataAnnotations.Schema;

namespace GetBangumiInfo.Models.Database;

[Table("episodeListVeryCold")]
public class EpisodeVeryCold
{
    [Column("subjectId")]
    public int SubjectId { get; set; }

    [Column("episode")]
    public float EpisodeNum { get; set; }

    [Column("addInDate")]
    public DateTime AddInDate { get; set; }

    [Column("isBilibili")]
    public bool IsBilibili { get; set; }
    
    [Column("Id")]
    public int Id { get; set; }
}
