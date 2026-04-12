// Als `using`:
//   using YepPet.Infrastructure.Persistence.Entities;

// Dins de la classe YepPetDbContext, després dels altres DbSet<>:
    public DbSet<CountryRecord> Countries => Set<CountryRecord>();

    public DbSet<CityRecord> Cities => Set<CityRecord>();
