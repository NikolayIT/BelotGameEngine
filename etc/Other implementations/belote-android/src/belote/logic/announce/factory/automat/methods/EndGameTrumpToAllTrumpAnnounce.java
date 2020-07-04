package belote.logic.announce.factory.automat.methods;

import belote.bean.Game;
import belote.bean.Player;
import belote.bean.announce.Announce;
import belote.logic.announce.factory.automat.methods.base.ConditionListMethod;
import belote.logic.announce.factory.automat.methods.conditions.OppositeTeamEndGameZone;
import belote.logic.announce.factory.automat.methods.conditions.TeamAttack;
import belote.logic.announce.factory.automat.methods.conditions.base.MultipleAndCondition;
import belote.logic.announce.factory.transformers.AnnounceTransformer;
import belote.logic.announce.factory.transformers.DoubleAnnounce;

public final class EndGameTrumpToAllTrumpAnnounce extends ConditionListMethod {

    /**
     * Constructor.
     * @param game BelotGame instance class.
     */
    public EndGameTrumpToAllTrumpAnnounce(final Game game) {
        super(game);
        
        addAnnounceCondition(new MultipleAndCondition(new OppositeTeamEndGameZone(game), new TeamAttack(game)));
    }

    /**
     * Returns the proper Announce when conditions match.
     * @param player who is on turn.
     * @return an Announce instance.
     */
    protected Announce createAnnounce(final Player player) {
        Player partner = player.getPartner();
        
        Announce playerAnnounce = game.getAnnounceList().getContractAnnounce(player);
        Announce partnerAnnounce = game.getAnnounceList().getContractAnnounce(partner);
        
        if (playerAnnounce != null && partnerAnnounce != null && playerAnnounce.isTrumpAnnounce() && partnerAnnounce.isTrumpAnnounce()) {
            AnnounceTransformer doubleAnnounce = new DoubleAnnounce(game);
            return doubleAnnounce.transform(player, Announce.createATNormalAnnounce(player));
        }

        return null;
    }
}