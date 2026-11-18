using DoggoDex.Api.Models;
using DoggoDex.Shared.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace DoggoDex.Api.Infrastructure;

public static class DbSeeder
{
    /// <summary>Dev-only password shared by every seeded account: "Password1!"</summary>
    public const string SeedPassword = "Password1!";

    public static async Task SeedAsync(AppDbContext context)
    {
        if (await context.Users.AnyAsync())
        {
            return;
        }

        var defaultHash = new PasswordHasher<User>().HashPassword(new User(), SeedPassword);

        // ==========================================
        // 1. USERS (Covering all 4 roles)
        // ==========================================
        var owner1User = new User
        {
            Id = Guid.NewGuid(),
            Email = "jared@doggodex.com",
            PasswordHash = defaultHash,
            Role = UserRole.Owner,
            CreatedAt = DateTime.UtcNow.AddDays(-30)
        };

        var owner2User = new User
        {
            Id = Guid.NewGuid(),
            Email = "camila.r@doggodex.com",
            PasswordHash = defaultHash,
            Role = UserRole.Owner,
            CreatedAt = DateTime.UtcNow.AddDays(-20)
        };

        var cafeUser = new User
        {
            Id = Guid.NewGuid(),
            Email = "hello@barkinglot.com",
            PasswordHash = defaultHash,
            Role = UserRole.Establishment,
            CreatedAt = DateTime.UtcNow.AddDays(-45)
        };

        var hotelUser = new User
        {
            Id = Guid.NewGuid(),
            Email = "concierge@pawsinn.com",
            PasswordHash = defaultHash,
            Role = UserRole.Establishment,
            CreatedAt = DateTime.UtcNow.AddDays(-40)
        };

        var communityUser = new User
        {
            Id = Guid.NewGuid(),
            Email = "neighbor.alex@community.com",
            PasswordHash = defaultHash,
            Role = UserRole.Community,
            CreatedAt = DateTime.UtcNow.AddDays(-15)
        };

        var adminUser = new User
        {
            Id = Guid.NewGuid(),
            Email = "admin@doggodex.com",
            PasswordHash = defaultHash,
            Role = UserRole.Admin,
            CreatedAt = DateTime.UtcNow.AddDays(-60)
        };

        context.Users.AddRange(owner1User, owner2User, cafeUser, hotelUser, communityUser, adminUser);

        // ==========================================
        // 2. OWNER PROFILES
        // ==========================================
        var jaredOwner = new Owner
        {
            Id = Guid.NewGuid(),
            UserId = owner1User.Id,
            Name = "Jared Samayoa",
            Bio = "Software developer and proud dog parent. Always carry extra waste bags and treats.",
            ProfilePhotoUrl = "https://images.unsplash.com/photo-1534528741775-53994a69daeb",
            AggregateRating = 4.90m,
            ReviewCount = 2,
            ShareableID = "jared-samayoa"
        };

        var camilaOwner = new Owner
        {
            Id = Guid.NewGuid(),
            UserId = owner2User.Id,
            Name = "Camila Reyes",
            Bio = "Weekend hiker with two energetic rescues.",
            ProfilePhotoUrl = "https://images.unsplash.com/photo-1517841905240-472988babdf9",
            AggregateRating = 3.60m,
            ReviewCount = 1,
            ShareableID = "camila-reyes"
        };

        context.Owners.AddRange(jaredOwner, camilaOwner);

        // ==========================================
        // 3. ESTABLISHMENT PROFILES
        // ==========================================
        var barkingLotCafe = new Establishment
        {
            Id = Guid.NewGuid(),
            UserId = cafeUser.Id,
            Name = "The Barking Lot Cafe",
            PetPolicy = "Dogs must remain on a 6ft leash on the outdoor patio. No dogs on furniture.",
            PerksDescription = "Complimentary pup-cup and organic biscuit for dogs rated 4.5 stars or higher!",
            ProfilePhotoUrl = "https://images.unsplash.com/photo-1554118811-1e0d58224f24"
        };

        var pawsInnHotel = new Establishment
        {
            Id = Guid.NewGuid(),
            UserId = hotelUser.Id,
            Name = "Paws & Stay Boutique Hotel",
            PetPolicy = "Up to 2 dogs per suite. Dogs cannot be left unattended in rooms if prone to barking.",
            PerksDescription = "Waived $50 pet cleaning fee for verified 4.8+ star owners and dogs.",
            ProfilePhotoUrl = "https://images.unsplash.com/photo-1566073771259-6a8506099945"
        };

        context.Establishments.AddRange(barkingLotCafe, pawsInnHotel);

        // ==========================================
        // 4. DOG PROFILES (Single & Multi-dog owners)
        // ==========================================
        var skyDog = new Dog
        {
            Id = Guid.NewGuid(),
            OwnerId = jaredOwner.Id,
            Name = "Sky",
            Breed = "Siberian Husky",
            Age = 3,
            ProfilePhotoUrl = "https://images.unsplash.com/photo-1605568427561-40dd23c2bbe4",
            AggregateRating = 4.90m,
            ReviewCount = 2,
            ShareableID = "sky-husky"
        };

        var maxDog = new Dog
        {
            Id = Guid.NewGuid(),
            OwnerId = camilaOwner.Id,
            Name = "Max",
            Breed = "Golden Retriever",
            Age = 5,
            ProfilePhotoUrl = "https://images.unsplash.com/photo-1552053831-71594a27632d",
            AggregateRating = 4.60m,
            ReviewCount = 1,
            ShareableID = "max-golden"
        };

        var lunaDog = new Dog
        {
            Id = Guid.NewGuid(),
            OwnerId = camilaOwner.Id,
            Name = "Luna",
            Breed = "Beagle",
            Age = 2,
            ProfilePhotoUrl = "https://images.unsplash.com/photo-1537151608828-ea2b11777ee8",
            AggregateRating = 2.80m,
            ReviewCount = 1,
            ShareableID = "luna-beagle"
        };

        context.Dogs.AddRange(skyDog, maxDog, lunaDog);

        await context.SaveChangesAsync();

        // ==========================================
        // 5. REVIEWS (Testing all scenarios)
        // ==========================================
        var reviews = new List<Review>
        {
            // Scenario A: Business review on a Dog (Approved, High Rating)
            new Review
            {
                Id = Guid.NewGuid(),
                ReviewerId = cafeUser.Id,
                OwnerId = null,
                DogId = skyDog.Id,
                LeashManners = 5,
                NoiseControl = 5,
                SocialReactivity = 5,
                OwnerAttentiveness = 5,
                Cleanliness = 5,
                WrittenFeedback = "Sky settled right under the table on the patio. Quiet, clean, and super polite around staff.",
                ReviewerType = ReviewerType.Business,
                CreatedAt = DateTime.UtcNow.AddDays(-10),
                ModerationStatus = ModerationStatus.Approved
            },

            // Scenario B: Community review on the same Dog (Approved, High Rating)
            new Review
            {
                Id = Guid.NewGuid(),
                ReviewerId = communityUser.Id,
                OwnerId = null,
                DogId = skyDog.Id,
                LeashManners = 5,
                NoiseControl = 4,
                SocialReactivity = 5,
                OwnerAttentiveness = 5,
                Cleanliness = 5,
                WrittenFeedback = "Met Sky at the local park—great recall and very friendly with smaller dogs.",
                ReviewerType = ReviewerType.Community,
                CreatedAt = DateTime.UtcNow.AddDays(-5),
                ModerationStatus = ModerationStatus.Approved
            },

            // Scenario C: Business review directly on an Owner (Approved)
            new Review
            {
                Id = Guid.NewGuid(),
                ReviewerId = hotelUser.Id,
                OwnerId = jaredOwner.Id,
                DogId = null,
                LeashManners = 5,
                NoiseControl = 5,
                SocialReactivity = 5,
                OwnerAttentiveness = 5,
                Cleanliness = 5,
                WrittenFeedback = "Jared followed all hotel pet guidelines, kept his dog leashed in the lobby, and left the suite spotless.",
                ReviewerType = ReviewerType.Business,
                CreatedAt = DateTime.UtcNow.AddDays(-8),
                ModerationStatus = ModerationStatus.Approved
            },

            // Scenario D: Owner-to-Owner/Dog review on Max (Approved)
            new Review
            {
                Id = Guid.NewGuid(),
                ReviewerId = owner1User.Id,
                OwnerId = null,
                DogId = maxDog.Id,
                LeashManners = 5,
                NoiseControl = 5,
                SocialReactivity = 4,
                OwnerAttentiveness = 5,
                Cleanliness = 4,
                WrittenFeedback = "Max is a gentle giant on the trails.",
                ReviewerType = ReviewerType.Owner,
                CreatedAt = DateTime.UtcNow.AddDays(-4),
                ModerationStatus = ModerationStatus.Approved
            },

            // Scenario E: Low-rating Business review on Luna (Approved - tests mixed multi-dog owner profile)
            new Review
            {
                Id = Guid.NewGuid(),
                ReviewerId = cafeUser.Id,
                OwnerId = null,
                DogId = lunaDog.Id,
                LeashManners = 3,
                NoiseControl = 2,
                SocialReactivity = 3,
                OwnerAttentiveness = 3,
                Cleanliness = 3,
                WrittenFeedback = "Luna barked continuously whenever waiters walked past the patio gate.",
                ReviewerType = ReviewerType.Business,
                CreatedAt = DateTime.UtcNow.AddDays(-2),
                ModerationStatus = ModerationStatus.Approved
            },

            // Scenario F: Flagged/Pending review on Camila (Tests Admin Moderation Queue)
            new Review
            {
                Id = Guid.NewGuid(),
                ReviewerId = communityUser.Id,
                OwnerId = camilaOwner.Id,
                DogId = null,
                LeashManners = 2,
                NoiseControl = 2,
                SocialReactivity = 2,
                OwnerAttentiveness = 1,
                Cleanliness = 2,
                WrittenFeedback = "This review has been flagged for moderator verification.",
                ReviewerType = ReviewerType.Community,
                CreatedAt = DateTime.UtcNow.AddHours(-6),
                ModerationStatus = ModerationStatus.Pending
            },

            // Scenario G: Hidden review (Tests exclusion from public display)
            new Review
            {
                Id = Guid.NewGuid(),
                ReviewerId = communityUser.Id,
                OwnerId = jaredOwner.Id,
                DogId = null,
                LeashManners = 1,
                NoiseControl = 1,
                SocialReactivity = 1,
                OwnerAttentiveness = 1,
                Cleanliness = 1,
                WrittenFeedback = "Spam test entry hidden by admin.",
                ReviewerType = ReviewerType.Community,
                CreatedAt = DateTime.UtcNow.AddDays(-12),
                ModerationStatus = ModerationStatus.Hidden
            }
        };

        context.Reviews.AddRange(reviews);
        await context.SaveChangesAsync();
    }
}