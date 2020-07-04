/*
 * Copyright (c) Dimitar Karamanov 2008-2014. All Rights Reserved.
 *
 * Redistribution and use in source and binary forms, with or without
 * modification, are permitted provided that the source code must retain
 * the above copyright notice and the following disclaimer.
 *
 * This software is provided "AS IS," without a warranty of any kind.
 */
package belote.logic.announce.factory.automat.methods.suitDeterminants;

import belote.bean.pack.card.rank.Rank;
import belote.logic.announce.factory.automat.methods.suitDeterminants.base.RankSuitDeterminant;

/**
 * Returns suit of which the player has Jack and Nine cards or null.
 * @author Dimitar Karamanov
 */
public final class JackNineSuit extends RankSuitDeterminant {

    public JackNineSuit() {
        addRank(Rank.Jack);
        addRank(Rank.Nine);
    }
}
