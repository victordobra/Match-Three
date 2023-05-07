using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using Unity.VisualScripting;
using UnityEngine;

public class GameManager : MonoBehaviour {
    public uint width = 5;
    public uint height = 5;
    public float maxDimension = 6;
    public float fallSpeed = 10;
    public uint scoreIncrement = 100;
    public GameObject[] piecePrefabs;

    public uint score = 0;

    private GameObject gridObject;
    private GameObject highlighter;
    private Camera cam;

    private float worldWidth;
    private float worldHeight;

    private GameObject[,] grid;

    private uint2 selectedPos;
    private bool selectedPiece = false;

    private bool animating = false;
    private uint multiplier = 1;

    private void Start() {
        // Find all needed game objects
        gridObject = GameObject.FindWithTag("Grid");
        highlighter = GameObject.FindWithTag("Highlighter");
        cam = Camera.main;

        // Calculate the world width and height
        if(width > height) {
            worldWidth = maxDimension;
            worldHeight = maxDimension * height / width;
        } else {
            worldWidth = maxDimension * width / height;
            worldHeight = maxDimension;
        }

        // Set the highlighter's scale and disable it
        float scale = worldWidth / (width - 1);

        highlighter.transform.localScale = new Vector3(scale * 1.2f, scale * 1.2f, 1);
        highlighter.SetActive(false);

        // Create the grid matrix
        grid = new GameObject[width, height];

        // Generate the piece grid
        GenerateGrid();
    }

    private void Update() {
        if(animating) {
            bool movedPiece = false;

            // Loop through all pieces
            for(uint i = 0; i != width; ++i)
                for(uint j = 0; j != height; ++j) {
                    GameObject piece = grid[i, j];

                    // Move the piece towards its target position
                    Vector3 target = GridToWorld(new uint2(i, j));

                    // Continue of the piece is at its target position
                    if(piece.transform.position == target)
                        continue;

                    movedPiece = true;

                    if(piece.transform.position.y - target.y < fallSpeed * Time.deltaTime)
                        piece.transform.Translate((piece.transform.position.y - target.y) * Vector3.down);
                    else
                        piece.transform.Translate(fallSpeed * Time.deltaTime * Vector3.down);
                }

            // Remove all matches if no piece was moved
            if(!movedPiece) {
                animating = false;
                RemoveMatchesGravity();
            }
        } else {
            multiplier = 1;

            // Check if the mouse left button was clicked
            if(Input.GetMouseButtonDown(0)) {
                if(!selectedPiece)
                    SelectPiece();
                else
                    SwapPieces();
            }

            // Move the highlighter to the selected piece
            if(selectedPiece)
                highlighter.transform.position = GridToWorld(selectedPos);

            // Enable or disable the highlighter
            highlighter.SetActive(selectedPiece);
        }
    }

    private uint2 WorldToGrid(Vector3 worldPos) {
        float scale = worldWidth / (width - 1);

        // Check if the world position is in range
        if (worldPos.x <= -(worldWidth + scale) * .5f || worldPos.x >= (worldWidth + scale) * .5f || worldPos.y <= -(worldHeight + scale) * .5f || worldPos.y >= (worldHeight + scale) * .5f)
            return new uint2(width, height);

        uint2 gridPos;

        // Calculate the closest grid position
        gridPos.x = (uint)((worldPos.x + (worldWidth + scale) * .5f) / (worldWidth + scale) * width);
        gridPos.y = (uint)((worldPos.y + (worldHeight + scale) * .5f) / (worldHeight + scale) * height);

        return gridPos;
    }
    private Vector3 GridToWorld(uint2 gridPos) {
        Vector3 worldPos;

        // Calculate the world position
        worldPos.x = worldWidth * ((float)gridPos.x / (width - 1) - .5f);
        worldPos.y = worldHeight * ((float)gridPos.y / (height - 1) - .5f);
        worldPos.z = 0;

        return worldPos;
    }
    private bool PiecesNeighbouring(uint2 piece1, uint2 piece2) {
        uint xDif = (piece1.x > piece2.x) ? (piece1.x - piece2.x) : (piece2.x - piece1.x);
        uint yDif = (piece1.y > piece2.y) ? (piece1.y - piece2.y) : (piece2.y - piece1.y);

        return xDif + yDif == 1;
    }
    private uint2[] GetMatches(uint2 piece) {
        uint2[] array = new uint2[0];

        // Look for rows of length at least 3 in the piece's column
        Sprite currentSprite = grid[piece.x, 0].GetComponent<SpriteRenderer>().sprite;
        uint length = 1;

        for(uint i = 1; i != height; ++i) {
            // Get the current sprite
            Sprite sprite = grid[piece.x, i].GetComponent<SpriteRenderer>().sprite;

            if(sprite == currentSprite)
                ++length;
            else {
                // Check if the string is long enough to erase
                if(length >= 3) {
                    // Save the array's old length
                    uint oldLength = (uint)array.Length;

                    // Resize the array
                    Array.Resize(ref array, (int)(array.Length + length));

                    // Add every value from the string to the list
                    for(uint j = 0; j != length; ++j)
                        array[oldLength + j] = new uint2(piece.x, i - length + j);
                }

                // Reset the length and the current sprite
                currentSprite = sprite;
                length = 1;
            }
        }
        // Check if the string is long enough to erase
        if(length >= 3) {
            // Save the array's old length
            uint oldLength = (uint)array.Length;

            // Resize the array
            Array.Resize(ref array, (int)(array.Length + length));

            // Add every value from the string to the list
            for(uint j = 0; j != length; ++j)
                array[oldLength + j] = new uint2(piece.x, height - length + j);
        }

        // Look for rows of length at least 3 in the piece's row
        currentSprite = grid[0, piece.y].GetComponent<SpriteRenderer>().sprite;
        length = 1;

        for(uint i = 1; i != width; ++i) {
            // Get the current sprite
            Sprite sprite = grid[i, piece.y].GetComponent<SpriteRenderer>().sprite;

            if(sprite == currentSprite)
                ++length;
            else {
                // Check if the string is long enough to erase
                if (length >= 3) {
                    // Save the array's old length
                    uint oldLength = (uint)array.Length;

                    // Resize the array
                    Array.Resize(ref array, (int)(array.Length + length));

                    // Add every value from the string to the list
                    for(uint j = 0; j != length; ++j)
                        array[oldLength + j] = new uint2(i - length + j, piece.y);
                }

                // Reset the length and the current sprite
                currentSprite = sprite;
                length = 1;
            }
        }
        // Check if the string is long enough to erase
        if(length >= 3) {
            // Save the array's old length
            uint oldLength = (uint)array.Length;

            // Resize the array
            Array.Resize(ref array, (int)(array.Length + length));

            // Add every value from the string to the list
            for(uint j = 0; j != length; ++j)
                array[oldLength + j] = new uint2(width - length + j, piece.y);
        }

        return array;
    }
    private uint2[] GetAllMatches() {
        uint2[] array = new uint2[0];

        // Loop through every column
        for(uint row = 0; row != width; ++row) {
            // Look for rows of length at least 3 in the current column
            Sprite currentSprite = grid[row, 0].GetComponent<SpriteRenderer>().sprite;
            uint length = 1;

            for(uint i = 1; i != height; ++i) {
                // Get the current sprite
                Sprite sprite = grid[row, i].GetComponent<SpriteRenderer>().sprite;

                if(sprite == currentSprite)
                    ++length;
                else {
                    // Check if the string is long enough to erase
                    if(length >= 3) {
                        // Save the array's old length
                        uint oldLength = (uint)array.Length;

                        // Resize the array
                        Array.Resize(ref array, (int)(array.Length + length));

                        // Add every value from the string to the list
                        for(uint j = 0; j != length; ++j)
                            array[oldLength + j] = new uint2(row, i - length + j);
                    }

                    // Reset the length and the current sprite
                    currentSprite = sprite;
                    length = 1;
                }
            }
            // Check if the string is long enough to erase
            if(length >= 3) {
                // Save the array's old length
                uint oldLength = (uint)array.Length;

                // Resize the array
                Array.Resize(ref array, (int)(array.Length + length));

                // Add every value from the string to the list
                for(uint j = 0; j != length; ++j)
                    array[oldLength + j] = new uint2(row, height - length + j);
            }
        }

        // Loop through every row
        for(uint col = 0; col != height; ++col) {
            // Look for rows of length at least 3 in the current row
            Sprite currentSprite = grid[0, col].GetComponent<SpriteRenderer>().sprite;
            uint length = 1;

            for(uint i = 1; i != width; ++i) {
                // Get the current sprite
                Sprite sprite = grid[i, col].GetComponent<SpriteRenderer>().sprite;

                if(sprite == currentSprite)
                    ++length;
                else {
                    // Check if the string is long enough to erase
                    if(length >= 3) {
                        // Save the array's old length
                        uint oldLength = (uint)array.Length;

                        // Resize the array
                        Array.Resize(ref array, (int)(array.Length + length));

                        // Add every value from the string to the list
                        for(uint j = 0; j != length; ++j)
                            array[oldLength + j] = new uint2(i - length + j, col);
                    }

                    // Reset the length and the current sprite
                    currentSprite = sprite;
                    length = 1;
                }
            }
            // Check if the string is long enough to erase
            if(length >= 3) {
                // Save the array's old length
                uint oldLength = (uint)array.Length;

                // Resize the array
                Array.Resize(ref array, (int)(array.Length + length));

                // Add every value from the string to the list
                for (uint j = 0; j != length; ++j)
                    array[oldLength + j] = new uint2(width - length + j, col);
            }
        }

        return array;
    }

    private GameObject GenerateRandomPiece() {
        // Generate the prefab's index
        uint index = (uint)UnityEngine.Random.Range(0, piecePrefabs.Length);

        // Instantiate the piece
        GameObject piece = Instantiate(piecePrefabs[index]);

        // Set its parent
        piece.transform.SetParent(gridObject.transform, true);

        return piece;
    }
    private void SetPieceTransform(GameObject piece, uint2 gridPos) {
        // Convert the grid pos to world positions
        Vector3 worldPos = GridToWorld(gridPos);

        // Calculate the tile's scale
        float scale = worldWidth / (width - 1);

        // Set the piece's transform
        piece.transform.position = worldPos;
        piece.transform.localScale = new Vector3(scale, scale, 1);
    }
    private void GenerateGrid() {
        // Create evety peice and set its transform
        for(uint i = 0; i != width; ++i)
            for(uint j = 0; j != height; ++j) {
                GameObject piece = GenerateRandomPiece();
                SetPieceTransform(piece, new uint2(i, j));

                grid[i, j] = piece;
            }

        // Remove all matches
        RemoveMatches();
    }
    private void GravityFill() {
        // Loop through every column
        for(uint i = 0; i != width; ++i) {
            // Save an index of the wanted location
            uint wantedLocation = 0;

            // Loop through the column
            for(uint j = 0; j != height; ++j) {
                // Pace the current piece in the wanted posotion
                GameObject piece = grid[i, j];
                grid[i, j] = null;
                grid[i, wantedLocation] = piece;

                // Check if the piece is goind to be moved
                if (piece != null && j != wantedLocation)
                    animating = true;

                if (piece != null) {
                    // Increment the wanted location
                    ++wantedLocation;
                }
            }

            float scale = worldWidth / (width - 1);

            // Fill the remaining space with random pieces
            for(uint j = wantedLocation; j != height; ++j) {
                animating = true;

                // Generate a random piece and put it in the wanted location
                GameObject piece = GenerateRandomPiece();
                SetPieceTransform(piece, new uint2(i, j));

                grid[i, j] = piece;

                // Move the piece up
                piece.transform.Translate((6 + scale * (j - wantedLocation) - piece.transform.position.y) * Vector3.up);
            }
        }
    }
    private void RemoveMatches() {
        // Get all matches
        uint2[] arr = GetAllMatches();

        while(arr.Length != 0) {
            // Replace every piece in the array with a random piece
            for(uint i = 0; i != arr.Length; ++i) {
                if(grid[arr[i].x, arr[i].y] == null)
                    continue;

                // Delete the piece
                Destroy(grid[arr[i].x, arr[i].y]);
                grid[arr[i].x, arr[i].y] = null;

                // Replace it with a random piece
                GameObject piece = GenerateRandomPiece();
                SetPieceTransform(piece, new uint2(arr[i].x, arr[i].y));

                grid[arr[i].x, arr[i].y] = piece;
            }

            // Get all matches
            arr = GetAllMatches();
        }
    }
    private bool RemoveMatchesGravity() {
        // Get all matches
        uint2[] arr = GetAllMatches();

        if(arr.Length == 0)
            return false;

        // Replace every piece in the array with a random piece
        for(uint i = 0; i != arr.Length; ++i) {
            if(grid[arr[i].x, arr[i].y] == null)
                continue;

            // Delete the piece
            Destroy(grid[arr[i].x, arr[i].y]);
            grid[arr[i].x, arr[i].y] = null;

            // Increase the score
            score += scoreIncrement * multiplier;
            ++multiplier;
        }

        // Gravity fill the grid
        GravityFill();

        return true;
    }

    private void SelectPiece() {
        // Select the piece currently clicked on
        uint2 currentPiece = WorldToGrid(cam.ScreenToWorldPoint(Input.mousePosition));

        // Check if a piece was clicked
        if(currentPiece.x == width)
            return;

        // Set the currently selected piece
        selectedPos = currentPiece;
        selectedPiece = true;
    }
    private void SwapPieces() {
        // Get the piece currently clicked on
        uint2 currentPiece = WorldToGrid(cam.ScreenToWorldPoint(Input.mousePosition));

        // Check if a piece was clicked
        if(currentPiece.x == width)
            return;

        // Deselect the selected piece
        selectedPiece = false;

        // Check if the two pieces are adjacent
        if(!PiecesNeighbouring(currentPiece, selectedPos))
            return;

        GameObject currentGO = grid[currentPiece.x, currentPiece.y];
        GameObject selectedGO = grid[selectedPos.x, selectedPos.y];

        // Swap the two pieces
        Vector3 auxPos = currentGO.transform.position;
        currentGO.transform.position = selectedGO.transform.position;
        selectedGO.transform.position = auxPos;

        grid[currentPiece.x, currentPiece.y] = selectedGO;
        grid[selectedPos.x, selectedPos.y] = currentGO;

        // Check for any match
        uint2[] arr1 = GetMatches(currentPiece);
        uint2[] arr2 = GetMatches(selectedPos);

        // Place the pieces in their original positions if no match was found
        if(arr1.Length == 0 && arr2.Length == 0) {
            auxPos = currentGO.transform.position;
            currentGO.transform.position = selectedGO.transform.position;
            selectedGO.transform.position = auxPos;

            grid[currentPiece.x, currentPiece.y] = currentGO;
            grid[selectedPos.x, selectedPos.y] = selectedGO;

            return;
        }

        // Delete every matched piece
        for(uint i = 0; i != arr1.Length; ++i) {
            // Check if the piece was already deleted
            if(grid[arr1[i].x, arr1[i].y] == null)
                continue;

            // Delete the piece
            Destroy(grid[arr1[i].x, arr1[i].y]);
            grid[arr1[i].x, arr1[i].y] = null;

            // Increase the score
            score += scoreIncrement * multiplier;
            ++multiplier;
        }

        // Fill the grid
        GravityFill();

        // Remove all new matches
        RemoveMatchesGravity();
    }
}
