package belote.logic.announce.factory.automat.methods;

import belote.bean.Game;
import belote.bean.Player;
import belote.bean.Team;
import belote.bean.announce.Announce;
import belote.logic.announce.factory.automat.base.AnnounceMethod;
import belote.logic.announce.factory.transformers.AnnounceTransformer;
import belote.logic.announce.factory.transformers.DoubleAnnounce;

public final class RegGameTrumpToAllTrumpAnnounce implements AnnounceMethod {
    
    private final Game game;

    /**
     * Constructor.
     * @param game BelotGame instance class.
     */
    public RegGameTrumpToAllTrumpAnnounce(final Game game) {
        this.game = game;
    }

    @Override
    public Announce getAnnounce(final Player player) {
        Player partner = player.getPartner();
        
        Announce playerAnnounce = game.getAnnounceList().getContractAnnounce(player);
        Announce partnerAnnounce = game.getAnnounceList().getContractAnnounce(partner);
        
        boolean oppositeTeamHasNotAnnounce = !hasOppositeTeamTrumpAnnounce(player);
        boolean teamAttack = player.isSameTeam(game.getDealAttackPlayer());
        
        if (playerAnnounce != null && partnerAnnounce != null && playerAnnounce.isTrumpAnnounce() && partnerAnnounce.isTrumpAnnounce()
                && (oppositeTeamHasNotAnnounce || teamAttack)) {
            AnnounceTransformer doubleAnnounce = new DoubleAnnounce(game);
            return doubleAnnounce.transform(player, Announce.createATNormalAnnounce(player));
        }

        return null;
    }
    
    private boolean hasOppositeTeamTrumpAnnounce(final Player player) {
        Team oppositeTeam = game.getOppositeTeam(player);
        for (int i = 0; i < oppositeTeam.getPlayersCount(); i++) {
            Announce oppositeTeamAnnounce = game.getAnnounceList().getContractAnnounce(oppositeTeam.getPlayer(i));
            if (oppositeTeamAnnounce != null && oppositeTeamAnnounce.isTrumpAnnounce()) {
                return true;
            }
        }
        
        return false;
    }
}