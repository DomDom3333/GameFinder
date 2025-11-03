namespace GameFinderApi.Services
{
    public class GameFilterService
    {
        public HashSet<string> FilterAndRandomizeGames(
            IEnumerable<string> userIds,
            Func<string, HashSet<string>?> getUserGames,
            Func<string, HashSet<string>?> getUserWishlists,
            bool includeWishlist,
            int minOwners,
            int minWishlisted)
        {
            // Build counts
            var ownerCounts = new Dictionary<string, int>(StringComparer.Ordinal);
            var wishCounts = new Dictionary<string, int>(StringComparer.Ordinal);
            
            foreach (string userId in userIds)
            {
                var owned = getUserGames(userId);
                if (owned != null)
                {
                    foreach (string g in owned)
                    {
                        ownerCounts[g] = ownerCounts.TryGetValue(g, out int c) ? c + 1 : 1;
                    }
                }
                
                if (includeWishlist)
                {
                    var wished = getUserWishlists(userId);
                    if (wished != null)
                    {
                        foreach (string g in wished)
                        {
                            wishCounts[g] = wishCounts.TryGetValue(g, out int c) ? c + 1 : 1;
                        }
                    }
                }
            }

            // Union of all candidates (owners and optionally wishlists)
            var candidates = new HashSet<string>(ownerCounts.Keys, StringComparer.Ordinal);
            if (includeWishlist)
            {
                foreach (string g in wishCounts.Keys) 
                    candidates.Add(g);
            }

            bool ownersActive = minOwners > 0;
            bool wishlistActive = includeWishlist && minWishlisted > 0;

            IEnumerable<string> filtered = candidates.Where(g =>
            {
                // Check owners threshold if active
                if (ownersActive)
                {
                    if (!ownerCounts.TryGetValue(g, out int oc) || oc < minOwners)
                        return false; // Fails owners threshold
                }
                
                // Check wishlist threshold if active
                if (wishlistActive)
                {
                    if (!wishCounts.TryGetValue(g, out int wc) || wc < minWishlisted)
                        return false; // Fails wishlist threshold
                }
                
                // If no thresholds are active at all, include all games from the candidate pool
                return true;
            });

            // Randomize order for fairness
            var randomized = filtered.OrderBy(_ => Guid.NewGuid()).ToHashSet(StringComparer.Ordinal);

            return randomized;
        }
    }
}

