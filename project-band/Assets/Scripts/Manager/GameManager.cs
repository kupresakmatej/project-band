using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public enum TurnState { Player, Enemy }
public enum Difficulty { Beginner, Normal, Hard }

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [SerializeField] private CardData[] allyDeck;
    [SerializeField] private CardData[] enemyDeck;
    [SerializeField] private CardUIBehaviour cardUIManager;
    [SerializeField] private Difficulty aiDifficulty = Difficulty.Beginner; // Set in Inspector

    private List<Player> allyTeam = new List<Player>();
    private List<Player> enemyTeam = new List<Player>();
    private TurnState currentTurn = TurnState.Player;
    private CardData[] currentHand;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        Player[] allPlayers = FindObjectsOfType<Player>();
        foreach (Player p in allPlayers)
        {
            if (p.CompareTag("Ally")) allyTeam.Add(p);
            else if (p.CompareTag("Enemy")) enemyTeam.Add(p);
        }

        StartPlayerTurn();
    }

    void Update()
    {
        if (currentTurn == TurnState.Player && Input.GetKeyDown(KeyCode.E))
        {
            EndPlayerTurn();
        }
    }

    private void StartPlayerTurn()
    {
        currentTurn = TurnState.Player;
        currentHand = DrawCards(allyDeck, 5);
        cardUIManager.SetHand(currentHand);
        cardUIManager.EnableInput(true);
        Debug.Log("Player's turn started.");
    }

    public void EndPlayerTurn()
    {
        cardUIManager.ResetDeck();
        cardUIManager.EnableInput(false);
        StartEnemyTurn();
    }

    private void StartEnemyTurn()
    {
        currentTurn = TurnState.Enemy;
        if (enemyDeck == null || enemyDeck.Length == 0 || enemyDeck.All(c => c == null))
        {
            Debug.LogError("Enemy deck is null or empty! Cannot draw AI hand. Ending turn.");
            EndEnemyTurn();
            return;
        }
        currentHand = DrawCards(enemyDeck, 5);
        Debug.Log($"Drew {currentHand.Length} cards for AI hand: {string.Join(", ", currentHand.Select(c => c?.cardName ?? "null"))}");
        if (currentHand.Length == 0 || currentHand.All(c => c == null))
        {
            Debug.LogError("Failed to draw valid AI hand. Ending turn.");
            EndEnemyTurn();
            return;
        }
        StartCoroutine(AIPlayTurn());
    }

    private IEnumerator AIPlayTurn()
    {
        if (currentHand == null || currentHand.Length == 0 || currentHand.All(c => c == null))
        {
            Debug.LogError("AI hand is empty or invalid. Ending turn.");
            EndEnemyTurn();
            yield break;
        }

        var groupedHand = currentHand.Where(c => c != null).GroupBy(c => c.cardType).ToList();
        Debug.Log($"AI turn started with {groupedHand.Count} card groups: {string.Join(", ", groupedHand.Select(g => g.First().cardName))}");

        while (groupedHand.Count > 0)
        {
            CardData bestCard = null;
            List<Player> bestTargets = new List<Player>();
            float bestScore = float.MinValue;

            foreach (var group in groupedHand)
            {
                CardData card = group.First();
                List<Player> validTargets = GetValidAITargets(card);
                Debug.Log($"Evaluating card {card.cardName} with {validTargets.Count} valid targets.");
                var (score, targets) = EvaluateAICardAction(card, validTargets);
                if (score > bestScore)
                {
                    bestScore = score;
                    bestCard = card;
                    bestTargets = targets;
                }
            }

            if (bestCard != null && bestTargets.Count > 0)
            {
                Debug.Log($"AI playing card: {bestCard.cardName} on {string.Join(", ", bestTargets.Select(t => t.name))}");
                ApplyAICardEffect(bestCard, bestTargets);
                currentHand = currentHand.Where(c => c == null || c.cardType != bestCard.cardType).ToArray();
                groupedHand = currentHand.Where(c => c != null).GroupBy(c => c.cardType).ToList();
                yield return new WaitForSeconds(1f);
            }
            else
            {
                Debug.LogWarning("No valid AI plays available. Ending turn.");
                break;
            }
        }

        EndEnemyTurn();
    }

    private void EndEnemyTurn()
    {
        currentHand = null;
        CheckGameOver();
        StartPlayerTurn();
    }

    private CardData[] DrawCards(CardData[] deck, int count)
    {
        if (deck == null || deck.Length == 0 || deck.All(c => c == null))
        {
            Debug.LogError("DrawCards: Deck is null or empty. Returning empty hand.");
            return new CardData[0];
        }
        CardData[] hand = new CardData[Mathf.Min(count, deck.Length)];
        for (int i = 0; i < hand.Length; i++)
        {
            int idx = Random.Range(0, deck.Length);
            hand[i] = deck[idx]; // Could be null if deck contains nulls
        }
        Debug.Log($"Drew hand: {string.Join(", ", hand.Select(c => c?.cardName ?? "null"))}");
        return hand;
    }

    private List<Player> GetValidAITargets(CardData card)
    {
        List<Player> targets = new List<Player>();
        bool isHealing = card.healthBonus > 0;
        bool isDamaging = card.damageBonus > 0;

        if (isHealing)
            targets.AddRange(enemyTeam.Where(p => p.IsAlive()));
        if (isDamaging)
            targets.AddRange(allyTeam.Where(p => p.IsAlive()));
        return targets;
    }

    private (float score, List<Player> targets) EvaluateAICardAction(CardData card, List<Player> validTargets)
    {
        if (validTargets.Count == 0) return (float.MinValue, new List<Player>());

        switch (aiDifficulty)
        {
            case Difficulty.Beginner:
                return EvaluateBeginner(card, validTargets);
            case Difficulty.Normal:
                return EvaluateNormal(card, validTargets);
            case Difficulty.Hard:
                return EvaluateHard(card, validTargets);
            default:
                return EvaluateBeginner(card, validTargets);
        }
    }

    private (float score, List<Player> targets) EvaluateBeginner(CardData card, List<Player> validTargets)
    {
        float score = 0f;
        bool isMulti = IsMultiTargetCard(card);
        List<Player> selected = new List<Player>();

        if (card.healthBonus > 0)
        {
            float maxHealNeed = enemyTeam.Max(p => p.IsAlive() ? (p.MaxHealth - p.Health) / (float)p.MaxHealth : 0f);
            score += card.healthBonus * maxHealNeed * 10f;
            selected.Add(enemyTeam.Where(p => p.IsAlive()).OrderBy(p => p.Health).FirstOrDefault());
        }
        else if (card.damageBonus > 0)
        {
            int targetCount = isMulti ? Mathf.Min(card.maxMultiTargets, validTargets.Count) : 1;
            score += card.damageBonus * targetCount;
            selected.AddRange(allyTeam.Where(p => p.IsAlive()).OrderBy(p => p.Health).Take(targetCount));
        }

        return (score, selected);
    }

    private (float score, List<Player> targets) EvaluateNormal(CardData card, List<Player> validTargets)
    {
        float score = 0f;
        bool isMulti = IsMultiTargetCard(card);
        List<Player> selected = new List<Player>();

        // Check team health state
        float teamHealthRatio = enemyTeam.Where(p => p.IsAlive()).Sum(p => p.Health) / (float)enemyTeam.Sum(p => p.MaxHealth);
        bool prioritizeHealing = teamHealthRatio < 0.6f || enemyTeam.Any(p => p.IsAlive() && p.Health < p.MaxHealth * 0.5f);

        if (card.healthBonus > 0 && prioritizeHealing)
        {
            // Heal lowest HP, weight by need
            var healTarget = enemyTeam.Where(p => p.IsAlive()).OrderBy(p => p.Health).FirstOrDefault();
            if (healTarget != null)
            {
                float healNeed = (healTarget.MaxHealth - healTarget.Health) / (float)healTarget.MaxHealth;
                score += card.healthBonus * healNeed * 15f; // Higher weight for healing
                selected.Add(healTarget);
            }
        }
        else if (card.damageBonus > 0)
        {
            // Target enemies that can be killed or low HP
            var targets = allyTeam.Where(p => p.IsAlive()).OrderBy(p => p.Health).ToList();
            int targetCount = isMulti ? Mathf.Min(card.maxMultiTargets, targets.Count) : 1;
            foreach (var t in targets.Take(targetCount))
            {
                float efficiency = card.damageBonus >= t.Health ? 1.5f : 1f; // Bonus for killing
                score += card.damageBonus * efficiency;
                selected.Add(t);
            }
        }

        return (score, selected);
    }

    private (float score, List<Player> targets) EvaluateHard(CardData card, List<Player> validTargets)
    {
        float score = 0f;
        bool isMulti = IsMultiTargetCard(card);
        List<Player> selected = new List<Player>();

        // Simulate team state after play
        float teamHealthRatio = enemyTeam.Where(p => p.IsAlive()).Sum(p => p.Health) / (float)enemyTeam.Sum(p => p.MaxHealth);
        bool prioritizeHealing = teamHealthRatio < 0.5f || enemyTeam.Any(p => p.IsAlive() && p.Health < p.MaxHealth * 0.4f);

        if (card.healthBonus > 0 && prioritizeHealing)
        {
            // Heal to keep key units alive
            var healTargets = enemyTeam.Where(p => p.IsAlive()).OrderBy(p => p.Health).ToList();
            if (healTargets.Count > 0)
            {
                var target = healTargets.First();
                float healNeed = (target.MaxHealth - target.Health) / (float)target.MaxHealth;
                score += card.healthBonus * healNeed * 20f;
                // Bonus for keeping team alive
                score += 10f * (enemyTeam.Count(p => p.IsAlive()) / (float)enemyTeam.Count);
                selected.Add(target);
            }
        }
        else if (card.damageBonus > 0)
        {
            // Simulate damage to minimize enemy survivors
            var targets = allyTeam.Where(p => p.IsAlive()).OrderBy(p => p.Health).ToList();
            int targetCount = isMulti ? Mathf.Min(card.maxMultiTargets, targets.Count) : 1;
            foreach (var t in targets.Take(targetCount))
            {
                // Bonus for kills, penalize overkill
                float efficiency = card.damageBonus >= t.Health ? 2f : (card.damageBonus > t.Health * 2 ? 0.5f : 1f);
                score += card.damageBonus * efficiency;
                selected.Add(t);
            }
            // Bonus for reducing enemy count
            int projectedKills = targets.Take(targetCount).Count(t => t.Health <= card.damageBonus);
            score += projectedKills * 10f;
        }

        return (score, selected);
    }

    private void ApplyAICardEffect(CardData card, List<Player> targets)
    {
        switch (card.cardType)
        {
            case PlayerType.Bass:
                if (card.healthBonus > 0 && targets[0].CompareTag("Enemy"))
                {
                    targets[0].Heal(card.healthBonus);
                    Debug.Log($"AI healed {targets[0].name} for {card.healthBonus}");
                }
                else if (card.damageBonus > 0 && targets[0].CompareTag("Ally"))
                {
                    targets[0].TakeDamage(card.damageBonus);
                    Debug.Log($"AI damaged {targets[0].name} for {card.damageBonus}");
                }
                break;
            case PlayerType.Drummer:
                if (card.damageBonus > 0 && targets.All(t => t.CompareTag("Ally")))
                {
                    foreach (var t in targets)
                    {
                        t.TakeDamage(card.damageBonus);
                        Debug.Log($"AI damaged {t.name} for {card.damageBonus}");
                    }
                }
                break;
            case PlayerType.Guitar:
                if (card.damageBonus > 0 && targets[0].CompareTag("Ally"))
                {
                    targets[0].TakeDamage(card.damageBonus);
                    Debug.Log($"AI damaged {targets[0].name} for {card.damageBonus}");
                }
                break;
            case PlayerType.Keyboard:
                if (card.damageBonus > 0 && targets[0].CompareTag("Ally"))
                {
                    targets[0].TakeDamage(card.damageBonus);
                    Debug.Log($"AI damaged {targets[0].name} for {card.damageBonus}");
                }
                else if (card.healthBonus > 0 && targets[0].CompareTag("Enemy"))
                {
                    targets[0].Heal(2 * card.healthBonus);
                    Debug.Log($"AI healed {targets[0].name} for {2 * card.healthBonus}");
                }
                break;
            case PlayerType.Singer:
                if (card.healthBonus > 0 && targets[0].CompareTag("Enemy"))
                {
                    targets[0].Heal(card.healthBonus);
                    Debug.Log($"AI healed {targets[0].name} for {card.healthBonus}");
                }
                else if (card.damageBonus > 0 && targets.All(t => t.CompareTag("Ally")))
                {
                    foreach (var t in targets)
                    {
                        int damage = card.damageBonus * (targets.Count == 1 ? 2 : 1);
                        t.TakeDamage(damage);
                        Debug.Log($"AI damaged {t.name} for {damage}");
                    }
                }
                break;
        }
    }

    private bool IsMultiTargetCard(CardData card)
    {
        return card.cardType == PlayerType.Drummer || card.cardType == PlayerType.Singer;
    }

    private void CheckGameOver()
    {
        if (allyTeam.All(p => !p.IsAlive()))
        {
            Debug.Log("Enemies win!");
        }
        else if (enemyTeam.All(p => !p.IsAlive()))
        {
            Debug.Log("Player wins!");
        }
    }

    public void SetDifficulty(string difficulty)
    {
        switch (difficulty.ToLower())
        {
            case "beginner": aiDifficulty = Difficulty.Beginner; break;
            case "normal": aiDifficulty = Difficulty.Normal; break;
            case "hard": aiDifficulty = Difficulty.Hard; break;
        }
    }

    public CardData[] GetAllyDeck() => allyDeck;
    public CardData[] GetEnemyDeck() => enemyDeck;
    public List<Player> GetAllyTeam() => allyTeam;
    public List<Player> GetEnemyTeam() => enemyTeam;
}