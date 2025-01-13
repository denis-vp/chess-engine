# **Chess Engine Documentation**

## **Overview**

This chess engine is developed in C# and is designed to play chess at a high level by implementing advanced search algorithms, evaluation functions, move generation, and parallelization techniques. It utilizes multithreading to improve search efficiency and employs transposition tables to avoid redundant computations.

---

## **Key Components**

### **1. Searcher**

The `Searcher` class is responsible for implementing the core search algorithm (Negamax with Alpha-Beta pruning) to find the best move in a given chess position. It uses iterative deepening to progressively search deeper into the game tree.

**Main Features**:
- Iterative deepening search
- Alpha-beta pruning for efficient search
- Transposition table for storing previously evaluated positions
- Quiescence search to handle tactical complications (captures, checks)
- Multi-threaded search using worker threads to explore different depths in parallel

**Important Methods**:
- `StartSearch()`: Begins the iterative deepening search, using multiple threads to search at different depths.
- `SearchMovesParallel()`: Core recursive search function with alpha-beta pruning, implemented to run in parallel.
- `QuiescenceSearch()`: Searches only capture moves to reach a quiet position.
- `EndSearch()`: Cancels the search in progress.

---

### **2. Move Generator**

The `MoveGenerator` and `MoveGeneratorParallel` classes handle move generation for the chess engine. They generate legal moves for all pieces, taking into account special rules like castling, en passant, and pawn promotion.

**Key Features**:
- Generates all legal moves for a given board position
- Handles special moves (castling, en passant, pawn promotion)
- Parallelized move generation using tasks for sliding pieces (rooks, bishops, queens), knights, and pawns

**Methods**:
- `GenerateMoves(Board board, bool includeQuietMoves)`: Generates all possible moves for the current player.
- `GenerateKingMoves()`, `GenerateSlidingMoves()`, `GenerateKnightMoves()`, `GeneratePawnMoves()`: Specialized methods to generate moves for each piece type.
- `ComputeAttackData()`: Precomputes attack data for enemy pieces, used to determine checks and pins.

---

### **3. Evaluation**

The `Evaluation` and `EvaluationParallel` classes provide a static evaluation function to score the chess position from the perspective of the side to move. The engine uses this score to determine the best moves during the search.

**Key Features**:
- Evaluates material balance, piece positions, king safety, and pawn structure
- Adjusts evaluation based on the game phase (opening, middlegame, endgame)
- Parallelized evaluation for white and black positions

**Important Methods**:
- `Evaluate(Board board)`: Returns a score for the board position.
- `CountMaterial(int colorIndex)`: Computes material value for a given color.
- `EvaluatePieceSquareTables()`: Evaluates piece-square tables to account for positional factors.
- `MopUpEval()`: Provides a "mop-up" score in endgames when one side has a large material advantage.

---

### **4. Transposition Table**

The `TranspositionTable` class is a hash table that stores previously evaluated positions to avoid redundant calculations during the search.

**Key Features**:
- Uses Zobrist hashing to uniquely identify board positions
- Supports exact evaluations, upper bounds, and lower bounds for alpha-beta pruning
- Reduces search time by reusing results of previously explored positions

**Important Methods**:
- `LookupEvaluation()`: Checks if a position has already been evaluated at a sufficient depth.
- `StoreEvaluation()`: Stores the evaluation result of a position in the transposition table.

---

### **5. Board Representation**

The `Board` class represents the chessboard, storing piece positions and game state. It provides methods to make and unmake moves, keeping track of castling rights, en passant squares, and repetition history.

**Key Features**:
- Efficient board representation using arrays for piece positions
- Tracks game state, including castling rights, en passant, and fifty-move rule
- Supports move execution (`MakeMove`) and retraction (`UnmakeMove`)
- Zobrist hashing for quick position identification

**Important Methods**:
- `MakeMove(Move move, bool inSearch)`: Executes a move on the board.
- `UnmakeMove(Move move, bool inSearch)`: Reverts a move on the board.
- `LoadPosition(string fen)`: Loads a position from a FEN string.
- `Clone()`: Creates a deep copy of the board for parallel search.

---

## **Parallelization Techniques**

1. **Parallel Move Generation**:  
   The `MoveGeneratorParallel` class uses multiple tasks to generate moves for sliding pieces, knights, and pawns in parallel, improving move generation speed.

2. **Parallel Evaluation**:  
   The `EvaluationParallel` class evaluates white and black positions concurrently using `Parallel.Invoke`.

3. **Threaded Search**:  
   The `Searcher` class creates multiple threads, each searching at different depths. A countdown event is used to synchronize the threads, ensuring all threads complete before the best move is selected.

---

## **Special Rules Handling**

1. **Castling**:  
   Castling moves are generated only when the king and rook have not moved, and there are no pieces between them. Additionally, the king cannot castle out of, through, or into check.

2. **En Passant**:  
   En passant captures are generated and executed according to the standard rules of chess. The engine ensures that en passant does not leave the king in check.

3. **Pawn Promotion**:  
   Pawn promotions are handled during move generation. The engine promotes pawns to queens by default but can promote to other pieces as specified.

---

