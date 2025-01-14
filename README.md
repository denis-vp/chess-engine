# **Chess Engine Documentation**

## **Overview**

This chess engine is developed in C# and is designed to play chess at a high level by implementing advanced search algorithms, evaluation functions, move generation, and parallelization techniques. It utilizes multithreading to improve search efficiency and employs transposition tables to avoid redundant computations.

---

## **Settings**

Using the `settings.json` file, you can configure various parameters for the app such as:
- `ScreenWidht` and `ScreenHeight`: The dimensions of the window.
- `IsPlayerWhite`: Whether the human player is playing as white.
- `BotThinkTimeSeconds`: The time the bot takes to make a move.
- `MaxBookPly`: The maximum number of plies to look up in the opening book.
- `InvertPerspective`: Whether to invert the board perspective.
- `EngineVsEngine`: Whether to run the engine in engine vs engine mode. This takes precedence over the `IsPlayerWhite` setting.
- `PrintSearch`: Whether to print search information to the console.
- `MoveGenerationParallel`: Whether to use parallel move generation.
- `EvaluationParallel`: Whether to use parallel evaluation.
- `SearchParallel`: Whether to use parallel search.
- `PrintMoveGenerationTime`: Whether to print move generation time to the console.
- `PrintEvaluationTime`: Whether to print evaluation time to the console.

---

## **User Interface (UI)**

The chess engine's user interface is built using **Raylib**, a simple and powerful library designed for graphics rendering and game development. The UI provides an intuitive and visually appealing way to interact with the chess engine.

**Key Features**:
- **2D Chessboard Rendering**: Displays the board and pieces in a clean, animated 2D interface.
- **Real-time Interaction**: Users can click and drag pieces to make moves, with legal moves highlighted dynamically.
- **Customizable Themes**: Allows users to switch between board and piece themes.
- **AI Integration**: Lets players compete against the engine, showing the engine's "thinking" process visually through the cli (e.g., depth, best move).

Raylib's simplicity and performance make it an ideal choice for creating a responsive and visually appealing UI, enhancing the overall user experience of the chess engine.

---

## **Key Components**

### **1. Searcher**

The `Searcher` and `SearcherParallel` classes are responsible for implementing the core search algorithm (Negamax with Alpha-Beta pruning) to find the best move in a given chess position. It uses iterative deepening to progressively search deeper into the game tree.

**Main Features**:
- Iterative deepening search
- Alpha-beta pruning for efficient search
- Transposition table for storing previously evaluated positions
- Quiescence search to handle tactical complications (captures, checks)
- Parallelized search at different depths using multiple threads

**Important Methods**:
- `StartSearch()`: Begins the iterative deepening search.
- `SearchMoves()`: Core recursive search function with alpha-beta pruning.
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
   The `SearcherParallel` class creates multiple threads, each searching at different depths. A countdown event is used to synchronize the threads, ensuring all threads complete before the best move is selected.

---

## **Parallelization Outcomes**
After parallelizing the move generation and evaluation functions, the engine slowed down.  
This is to be expected as the overhead of creating and managing tasks can outweigh the benefits of parallelization for smaller tasks. The tasks are lightweight and the overhead of creating and managing them can be significant, especially when the tasks are short-lived.

---

## **Special Rules Handling**

1. **Castling**:  
   Castling moves are generated only when the king and rook have not moved, and there are no pieces between them. Additionally, the king cannot castle out of, through, or into check.

2. **En Passant**:  
   En passant captures are generated and executed according to the standard rules of chess. The engine ensures that en passant does not leave the king in check.

3. **Pawn Promotion**:  
   Pawn promotions are handled during move generation. The engine promotes pawns to queens by default but can promote to other pieces as specified.

---
