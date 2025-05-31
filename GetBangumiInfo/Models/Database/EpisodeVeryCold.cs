using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GetBangumiInfo.Models.Database;

[Table("episodeListVeryCold")]
public class EpisodeVeryCold
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("subjectId")]
    public int SubjectId { get; set; }

    [Column("episode")]
    public float EpisodeNum { get; set; }

    [Column("addInDate")]
    public DateTime AddInDate { get; set; }
}
